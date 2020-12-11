using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Profiling;
using Unity.Collections.LowLevel.Unsafe;



public interface ICommand
{
    void Run();
}

public class CommandBuffer
{

    public CommandBuffer(int size_in_bytes = 16 * 1024)
    {
        m_buffer = new byte[size_in_bytes];
    }

    public void WriteCommand<T>(T command) where T : struct, ICommand
    {
        CommandHandlerManager.WriteCommand(command, m_buffer, ref m_position);
    }

    public bool TryGetCommandBuffer(out byte[] command_buffer)
    {
        if(m_position > 0)
        {
            command_buffer = new byte[m_position];
            Array.Copy(m_buffer, command_buffer, m_position);
            m_position = 0;
            return true;
        }
        else
        {
            command_buffer = null;
            return false;
        }
    }

    struct CommandTypeInfo
    {
        public int m_size_in_bytes;
        public int m_id;
    }


    byte[] m_buffer;
    int m_position;

}

static class CommandHandlerManager
{
    static CommandHandlerManager()
    {
        Registerhandler<AddSolidDensityCommand>();
    }

    static void Registerhandler<T>() where T : struct, ICommand
    {
        var command_id = m_command_ids_to_handlers.Count;
        var handler = new CommandHandler<T>(command_id);
        m_command_ids_to_handlers[command_id] = handler;
        m_command_types_to_handlers[typeof(T)] = handler;
    }

    public static void WriteCommand<T>(T command, byte[] buffer, ref int position) where T : struct, ICommand
    {
        if(m_command_types_to_handlers.TryGetValue(typeof(T), out var handler))
        {
            unsafe
            {
                fixed(byte* buffer_bytes = &buffer[position])
                {
                    var buffer_ints = (int*)buffer_bytes;
                    *buffer_ints = handler.GetId();
                }
                position += 4;
            }

            var cast_handler = (CommandHandler<T>)handler;
            cast_handler.WriteCommand(command, buffer, ref position);
        }
        else
        {
            throw new Exception($"Could not find command handler for type {typeof(T).Name}");
        }
    }

    public static void RunCommands(byte[] buffer, int offset, int buffer_length)
    {
        while (offset < buffer_length)
        {
            RunCommand(buffer, ref offset);
        }
    }

    public static void RunCommand(byte[] buffer, ref int position)
    {
        int command_id = 0;
        unsafe
        {
            fixed (byte* buffer_bytes = &buffer[position])
            {
                var buffer_ints = (int*)buffer_bytes;
                command_id = *buffer_ints;
            }
            position += 4;
        }

        if (m_command_ids_to_handlers.TryGetValue(command_id, out var handler))
        {
            handler.RunCommand(buffer, ref position);
        }
        else
        {
            throw new Exception($"Could not find command handler for id {command_id}");
        }
    }

    abstract class CommandHandler
    {
        public CommandHandler(int command_id)
        {
            m_command_id = command_id;
        }

        public int GetId() { return m_command_id; }

        public abstract void RunCommand(byte[] command_buffer, ref int position);

        int m_command_id;
    }

    class CommandHandler<T> : CommandHandler where T : struct, ICommand
    {
        public CommandHandler(int command_id)
        :   base(command_id)
        {
            m_command_size_in_bytes = UnsafeUtility.SizeOf<T>();
        }

        public void WriteCommand(T command, byte[] buffer, ref int position)
        {
            if (position + m_command_size_in_bytes > buffer.Length)
            {
                throw new Exception($"Command buffer for command {typeof(T).Name} ran out of room. BufferLength={buffer.Length}, command_size_in_bytes={m_command_size_in_bytes}, m_position={position}");
            }

            unsafe
            {
                fixed(byte* byte_ptr = &buffer[position])
                {
                    UnsafeUtility.CopyStructureToPtr(ref command, byte_ptr);
                }
            }

            position += m_command_size_in_bytes;
        }

        public override void RunCommand(byte[] buffer, ref int position)
        {
            if (position + m_command_size_in_bytes > buffer.Length)
            {
                throw new Exception($"Not enough data inn command buffer for command {typeof(T).Name}. BufferLength={buffer.Length}, command_size_in_bytes={m_command_size_in_bytes}, m_position={position}");
            }

            var command = default(T);
            unsafe
            {
                fixed (byte* byte_ptr = &buffer[position])
                {
                    //UnsafeUtility.CopyPtrToStructure(byte_ptr, &command);
                }
            }

            command.Run();

            position += m_command_size_in_bytes;
        }

        int m_command_size_in_bytes;
    }

    static Dictionary<Type, CommandHandler> m_command_types_to_handlers = new Dictionary<Type, CommandHandler>();
    static Dictionary<int, CommandHandler> m_command_ids_to_handlers = new Dictionary<int, CommandHandler>();
}