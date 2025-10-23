using System;
using UnityEngine;

// helper class for storing player input
[Serializable]
public class PlayerInput
{
  public string horizontalName = "Horizontal";
  public string verticalName = "Vertical";
  public string jumpName = "Jump";

  public float horizontal { get; private set; }
  public float vertical { get; private set; }
  public Vector2 axisDirection { get; private set; }
  public bool jumpAction { get; private set; }
  public bool jumpActionDown { get; private set; }
  public bool jumpActionUp { get; private set; }

  public void InputUpdate()
  {
    AxisUpdate();
    ActionUpdate();
  }

  private void AxisUpdate()
  {
    horizontal = Input.GetAxis(horizontalName);
    vertical = Input.GetAxis(verticalName);
    axisDirection = new Vector2(horizontal, vertical);
  }

  private void ActionUpdate()
  {
    jumpActionDown = false;
    jumpActionUp = false;
    if (Input.GetButton(jumpName))
        {
          if (!jumpAction)
            jumpActionDown = true;
          jumpAction = true;
        }
        else if (jumpAction)
        {
          jumpAction = false;
          jumpActionUp = true;
        }
  }
}
