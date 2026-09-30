using Demo3D.Native;
using Demo3D.Visuals;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;

// Created by Mark Emeott
// Updated June 5, 2026

namespace Demo3D.Components._3DControlPanels {
    [Auto] public class SafetyMat : NativeObject {
        public SafetyMat(Visual sender) : base(sender) { Setup(sender); }

        #region Visual References
        [Auto] BoxVisual MatSensor;
        #endregion

        #region Custom Properties
        [Auto, Category("-About"), ReadOnly(true), Description("Version")]
        protected SimplePropertyValue<String> Version;
        [Auto, Category("-Configuration - Nametag"), Description("Show nametag"), DefaultValue(true)]
        public SimplePropertyValue<Boolean> ShowNametag;
        [Auto, Category("-Wiring In"), Description("Channel A Power ")]
        public SimplePropertyValue<Boolean> R_ChA_Power;
        [Auto, Category("-Wiring In"), Description("Channel B Power")]
        public SimplePropertyValue<Boolean> R_ChB_Power;
        [Auto, Category("-Wiring Out"), ReadOnly(true), Description("Channel A Safety Clear")]
        public SimplePropertyValue<Boolean> W_ChA_SafetyClear;
        [Auto, Category("-Wiring Out"), ReadOnly(true), Description("Channel B Safety Clear")]
        public SimplePropertyValue<Boolean> W_ChB_SafetyClear;
        #endregion

        #region Events
        [Auto] protected void OnReset(Visual sender) {
            Setup(sender);
            // reset sensor
            UpdateSensor();
        }

        private void Setup(Visual sender) {
            // update visual references
            MatSensor = sender.Children.Where(v => v.Type == "LightSensor").FirstOrDefault() as BoxVisual;
            // update version
            Version.Value = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            // setup listeners for sensor's onblocked & oncleared events
            MatSensor.OnBlocked.Clear();
            MatSensor.OnBlocked.NativeListeners -= MatSensor_OnBlocked;
            MatSensor.OnBlocked.NativeListeners += MatSensor_OnBlocked;
            MatSensor.OnCleared.Clear();
            MatSensor.OnCleared.NativeListeners -= MatSensor_OnCleared;
            MatSensor.OnCleared.NativeListeners += MatSensor_OnCleared;
        }

        [Auto] protected void OnShowNametagUpdated(Visual sender, Boolean value, Boolean oldValue) {
            var nametag = sender.FindImmediateChild("Nametag") as TextVisual;
            if (nametag != null) {
                nametag.Visible = ShowNametag.Value;
            }
        }

        // call update sensor function
        private void MatSensor_OnBlocked(PhysicsObject sender, Visual load) { UpdateSensor(); }
        private void MatSensor_OnCleared(PhysicsObject sender, Visual load) { UpdateSensor(); }
        [Auto] protected void OnR_ChA_PowerUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateSensor(); }
        [Auto] protected void OnR_ChB_PowerUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateSensor(); }

        private void UpdateSensor() {
            var safetyMat = MatSensor.Parent as ContainerVisual;
            if (R_ChA_Power.Value || R_ChB_Power.Value) {
                // powered
                safetyMat.Material.Color = R_ChA_Power.Value && R_ChB_Power.Value ? Color.Lime : Color.DarkGreen;
                if (MatSensor.IsBlocked) {
                    // sensor blocked
                    MatSensor.Material.Color = Color.Red;
                    W_ChA_SafetyClear.Value = false;
                    W_ChB_SafetyClear.Value = false;
                }
                else {
                    // sensor cleared
                    MatSensor.Material.Color = Color.Gainsboro;
                    W_ChA_SafetyClear.Value = R_ChA_Power.Value;
                    W_ChB_SafetyClear.Value = R_ChB_Power.Value;
                }
            }
            else {
                // not powered
                safetyMat.Material.Color = Color.Gray;
                MatSensor.Material.Color = Color.Gainsboro;
                W_ChA_SafetyClear.Value = false;
                W_ChB_SafetyClear.Value = false;
            }
            // refresh properties grid
            Utilities.RefreshPropertiesGrid(Visual);
        }

        [Auto] protected void OnAfterPropertyUpdated(Visual visual, String name) {
            if(name == "Depth" || name == "Height" || name == "Width" || name == "Thickness" || name.Contains("Nametag")) {
                var sender = visual as ContainerVisual;
                // update sensor size to inside Safety mat
                MatSensor.Height = sender.Height;
                MatSensor.Width = sender.Width - sender.Thickness * 2 - 0.001;
                // update nametag location
                UpdateNametagLocation(sender);
            }
        }

        private void UpdateNametagLocation(Visual visual) {
            //update nametag location
            var sender = visual as ContainerVisual;
            TextVisual nametag = sender.FindImmediateChild("Nametag") as TextVisual;
            if (nametag != null) {
                nametag.Location = vector(0.0, 0.0, 0.0);
            }
        }
        #endregion
    }
}