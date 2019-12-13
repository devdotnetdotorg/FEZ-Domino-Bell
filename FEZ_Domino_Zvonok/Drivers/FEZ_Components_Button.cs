/*
Copyright 2010 GHI Electronics LLC
Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
http://www.apache.org/licenses/LICENSE-2.0
Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. 
*/

using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using GHIElectronics.NETMF.FEZ;

namespace GHIElectronics.NETMF.FEZ
{
    public static partial class FEZ_Components
    {

        public class Button : IDisposable
        {
            InputPort button;

            public Button(FEZ_Pin.Digital pin)
            {
                button = new InputPort((Cpu.Pin)pin, false, Port.ResistorMode.PullUp);
            }

            public Button(FEZ_Pin.Interrupt pin)
            {
                button = new InterruptPort((Cpu.Pin)pin, true, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeBoth);
                button.OnInterrupt += new NativeEventHandler(OnInterrupt);
            }

            void OnInterrupt(uint data1, uint data2, DateTime time)
            {
				ButtonState state = (data2 == 0) ? ButtonState.Pressed : ButtonState.NotPressed;
				ButtonPressEvent((FEZ_Pin.Interrupt)data1, state);
            }

            public void Dispose()
            {
                button.Dispose();
            }

            public enum ButtonState : byte
            {
                NotPressed = 0,
                Pressed = 1,
            }

            public ButtonState GetState()
            {
                return (button.Read() == false) ? ButtonState.Pressed : ButtonState.NotPressed;
            }

            public delegate void ButtonPressEventHandler(FEZ_Pin.Interrupt pin, ButtonState state);
            public event ButtonPressEventHandler ButtonPressEvent = delegate { };
        }
    }
}