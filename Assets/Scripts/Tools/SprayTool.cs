using UnityEngine;

public class SprayTool : Tool
{
    public SprayTool(KeyCode key_code, float liquid_fill_rate)
    :   base(key_code)
    {
        m_liquid_fill_rate = liquid_fill_rate;
    }

    public override void LateUpdate(float dt)
    {
        var command = new AddLiquidDensityCommand
        {
            m_position = transform.position,
            m_amount = m_liquid_fill_rate * dt
        };

        Game.Instance.SendCommand(command);
        command.Run();
    }

    float m_liquid_fill_rate;
}