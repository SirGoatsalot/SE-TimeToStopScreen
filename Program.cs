﻿using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Windows;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // In order to add a new utility class, right-click on your project, 
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area. Name it in the box below, and
        // press OK. This utility class will be merged in with your code when
        // deploying your final script.
        //
        // You can also simply create a new utility class manually, you don't
        // have to use the template if you don't want to. Just do so the first
        // time to see what a utility class looks like.
        // 
        // Go to:
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
        //
        // to learn more about ingame scripts.

        IMyTextSurface _drawingSurface;
        RectangleF _viewport;
        IMyCockpit _controlSeat;
        List<IMyThrust> _thrusters;

        public Program()
        {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            //     
            // The constructor is optional and can be removed if not
            // needed.
            // 
            // It's recommended to set Runtime.UpdateFrequency 
            // here, which will allow your script to run itself without a 
            // timer block.

            // Set up drawing surface and viewport
            _controlSeat = GridTerminalSystem.GetBlockWithName("Control Seat [TTS]") as IMyCockpit;
            _drawingSurface = _controlSeat.GetSurface(0);
            Echo(_drawingSurface.ToString());
            _viewport = new RectangleF(
                (_drawingSurface.TextureSize - _drawingSurface.SurfaceSize) / 2f,
                _drawingSurface.SurfaceSize
            );

            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            _thrusters = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(_thrusters);
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            // The main entry point of the script, invoked every time
            // one of the programmable block's Run actions are invoked,
            // or the script updates itself. The updateSource argument
            // describes where the update came from. Be aware that the
            // updateSource is a  bitfield  and might contain more than 
            // one update type.
            // 
            // The method itself is required, but the arguments above
            // can be removed if not needed.

            if ((updateSource & UpdateType.Update10) != 0)
            {
                var frame = _drawingSurface.DrawFrame();

                drawSpritesTTS(ref frame);
                // drawSpritesDockSensor(ref frame);

                frame.Dispose();
            }
        }

        /// <summary>
        /// Draws all sprites for the current frame.
        /// </summary>
        private void drawSpritesTTS(ref MySpriteDrawFrame frame)
        {
            // Add sprite for background
            var sprite = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "Grid",
                Position = _viewport.Center,
                Size = _viewport.Size,
                Color = _drawingSurface.ScriptForegroundColor.Alpha(0.66f),
                Alignment = TextAlignment.CENTER
            };
            frame.Add(sprite);

            var position = new Vector2(_drawingSurface.SurfaceSize.X / 4, 5) + _viewport.Position;
            List<double> TTSandDTS = timeToStop();

            // Time-To-Stop Title
            sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = "| TTS |",
                Position = position,
                RotationOrScale = 0.8f,
                Color = Color.White,
                Alignment = TextAlignment.CENTER,
                FontId = "White"
            };
            frame.Add(sprite);
            position += new Vector2(0, 30);

            // Time-To-Stop
            sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = TTSandDTS[0].ToString() + " s",
                Position = position,
                RotationOrScale = 1.0f,
                Color = Color.White,
                Alignment = TextAlignment.CENTER,
                FontId = "White"
            };
            frame.Add(sprite);
            position += new Vector2(_drawingSurface.SurfaceSize.X / 2, -30);

            // Distance To Stop Title
            sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = " | DTS | ",
                Position = position,
                RotationOrScale = 0.8f,
                Color = Color.White,
                Alignment = TextAlignment.CENTER,
                FontId = "White"
            };
            frame.Add(sprite);
            position += new Vector2(0, 30);

            sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = TTSandDTS[1].ToString() + " m",
                Position = position,
                RotationOrScale = 1.0f,
                Color = Color.White,
                Alignment = TextAlignment.CENTER,
                FontId = "White"
            };
            frame.Add(sprite);
        }

        private void drawSpritesDockSensor(ref MySpriteDrawFrame frame)
        {
            List<IMyShipConnector> connectors = new List<IMyShipConnector>();
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(connectors);

            var position = new Vector2(_drawingSurface.SurfaceSize.X / 8, 6) + _viewport.Position;
            var sprite = new MySprite()
            { 
                Type = SpriteType.TEXT,
                Data = " | Connector Status | ",
                Position = position,
                RotationOrScale = 0.2f,
                Color = Color.White,
                Alignment = TextAlignment.CENTER,
                FontId = "White"
            };
            frame.Add(sprite);
            position += new Vector2(0, 30);

            foreach (IMyShipConnector connector in connectors)
            {
                if (connector.IsSameConstructAs(Me))
                {
                    switch (connector.Status)
                    {
                        case MyShipConnectorStatus.Connectable:
                            sprite = new MySprite()
                            {
                                Type = SpriteType.TEXT,
                                Data = "In Range",
                                Position = position,
                                RotationOrScale = 0.2f,
                                Color = Color.Yellow,
                                Alignment = TextAlignment.CENTER,
                                FontId = "White"
                            };
                            frame.Add(sprite);
                            position += new Vector2(0, 20);
                            break;

                        case MyShipConnectorStatus.Connected:
                            sprite = new MySprite()
                            {
                                Type = SpriteType.TEXT,
                                Data = "Connected To",
                                Position = position,
                                RotationOrScale = 0.2f,
                                Color = Color.Green,
                                Alignment = TextAlignment.CENTER,
                                FontId = "White"
                            };
                            frame.Add(sprite);
                            position += new Vector2(0, 20);
                            sprite = new MySprite()
                            {
                                Type = SpriteType.TEXT,
                                Data = connector.OtherConnector.CubeGrid.CustomName,
                                Position = position,
                                RotationOrScale = 0.2f,
                                Color = Color.Green,
                                Alignment = TextAlignment.CENTER,
                                FontId = "White"
                            };
                            frame.Add(sprite);
                            position += new Vector2(0, 20);
                            break;

                        case MyShipConnectorStatus.Unconnected:
                            sprite = new MySprite()
                            {
                                Type = SpriteType.TEXT,
                                Data = "Disconnected",
                                Position = position,
                                RotationOrScale = 0.2f,
                                Color = Color.Blue,
                                Alignment = TextAlignment.CENTER,
                                FontId = "White"
                            };
                            frame.Add(sprite);
                            position += new Vector2(0, 20);
                            break;

                        default:
                            sprite = new MySprite()
                            {
                                Type = SpriteType.TEXT,
                                Data = connector.CustomName + " Error",
                                Position = position,
                                RotationOrScale = 0.2f,
                                Color = Color.Red,
                                Alignment = TextAlignment.CENTER,
                                FontId = "White"
                            };
                            frame.Add(sprite);
                            position += new Vector2(0, 20);
                            break;
                    }
                }
            }
        }


        /// <summary>
        /// Calculates the current Time-To-Stop and other current physics values of <c>_controlSeat</c>'s grid, based on current change in momentum and velocity, and returns it in a list of doubles.
        /// </summary>
        /// <returns>
        /// A list of doubles where the indexes are as follows:
        /// 0 - Current Time-To-Stop in seconds.
        /// 1 - Current Time-To-Top in seconds.
        /// 2 - Current acceleration in m/s^2.
        /// 3 - Current velocity in m/s
        /// 4 - Current momentum in kg*m/s.
        /// </returns>
        private List<double> timeToStop()
        {
            List<double> result = new List<double>();
            double speed = _controlSeat.GetShipSpeed();
            Vector3D velocities = _controlSeat.GetShipVelocities().LinearVelocity;
            velocities.Normalize();
            double currentStoppingThrust = 0;

            foreach (IMyThrust thruster in _thrusters)
            {
                if (new Vector3D(thruster.GridThrustDirection) != velocities) { continue; }
                currentStoppingThrust += thruster.MaxEffectiveThrust;
            }

            double currentStoppingAcceleration = currentStoppingThrust / _controlSeat.CalculateShipMass().TotalMass;
            double time = speed / currentStoppingAcceleration;
            double distance = time * speed;
            result.Add(time);
            result.Add(distance);
            return result;
        }
    }
}
