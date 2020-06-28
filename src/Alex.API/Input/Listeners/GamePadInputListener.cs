﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Alex.API.Input.Listeners
{
    public class GamePadInputListener : InputListenerBase<GamePadState, Buttons>, ICursorInputListener
    {
        private GamePadCapabilities _gamePadCapabilities;

        public bool IsConnected => CurrentState.IsConnected;
        
        public GamePadInputListener(PlayerIndex playerIndex) : base(playerIndex)
        {
            RegisterMap(InputCommand.MoveForwards, Buttons.LeftThumbstickUp);
            RegisterMap(InputCommand.MoveBackwards, Buttons.LeftThumbstickDown);
            RegisterMap(InputCommand.MoveLeft, Buttons.LeftThumbstickLeft);
            RegisterMap(InputCommand.MoveRight, Buttons.LeftThumbstickRight);
            //RegisterMap(InputCommand.MoveUp, Buttons.A);
            //RegisterMap(InputCommand.MoveDown, Buttons.B);

           // RegisterMap(InputCommand.MoveSpeedIncrease, Buttons.RightTrigger);
           // RegisterMap(InputCommand.MoveSpeedDecrease, Buttons.LeftTrigger);
           // RegisterMap(InputCommand.MoveSpeedReset, Buttons.LeftStick);
           
            RegisterMap(InputCommand.LeftClick, Buttons.RightTrigger);
            RegisterMap(InputCommand.RightClick, Buttons.LeftTrigger);

        //    RegisterMap(InputCommand.ToggleFog, Buttons.X);
            RegisterMap(InputCommand.Exit, Buttons.Start);
            RegisterMap(InputCommand.ToggleChat, Buttons.Back);

            RegisterMap(InputCommand.ToggleCamera, Buttons.LeftStick);

            RegisterMap(InputCommand.HotBarSelectPrevious, Buttons.LeftShoulder);
            RegisterMap(InputCommand.HotBarSelectNext, Buttons.RightShoulder);
            
            RegisterMap(InputCommand.LookUp, Buttons.RightThumbstickUp);
            RegisterMap(InputCommand.LookDown, Buttons.RightThumbstickDown);
            RegisterMap(InputCommand.LookLeft, Buttons.RightThumbstickLeft);
            RegisterMap(InputCommand.LookRight, Buttons.RightThumbstickRight);
            
            RegisterMap(InputCommand.Jump, Buttons.A);
            RegisterMap(InputCommand.Sneak, Buttons.RightStick);
            
            RegisterMap(InputCommand.MoveUp, Buttons.DPadUp);
            RegisterMap(InputCommand.MoveDown, Buttons.DPadDown);
        }

        protected override GamePadState GetCurrentState()
        {
            return GamePad.GetState(PlayerIndex);
        }

        protected override bool IsButtonDown(GamePadState state, Buttons buttons)
        {
            return state.IsButtonDown(buttons);
        }

        protected override bool IsButtonUp(GamePadState state, Buttons buttons)
        {
            return state.IsButtonUp(buttons);
        }

        protected override void OnUpdate(GameTime gameTime)
        {
            _gamePadCapabilities = GamePad.GetCapabilities(PlayerIndex);
        }

        /// <inheritdoc />
        public Vector2 GetCursorPositionDelta()
        {
            return CurrentState.ThumbSticks.Right - PreviousState.ThumbSticks.Right;
        }

        /// <inheritdoc />
        public Vector2 GetCursorPosition()
        {
            return CurrentState.ThumbSticks.Right;
        }
    }
}
