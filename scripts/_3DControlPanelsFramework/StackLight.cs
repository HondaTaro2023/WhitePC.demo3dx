using System;
using System.ComponentModel;
using Demo3D.Native;
using Demo3D.Visuals;
using System.Reflection;
using Demo3D.WiringDiagram;
using Demo3D.Utilities;

// Created by Mark Emeott
// Updated June 30, 2026

namespace Demo3D.Components._3DControlPanels {
    [Auto] public class StackLight : NativeObject {
        public StackLight(Visual sender) : base(sender) { Setup(sender); }

        #region Custom Properties
        [Auto, Category("-About"), ReadOnly(true), Description("Version")]
        protected SimplePropertyValue<String> Version;
        [Auto, Category("-Configuration - StackLight"), Description("Number of lights on stack light"), DefaultValue(1)]
        public SimplePropertyValue<Int32> NumberOfLights;
        [Auto, Category("-Configuration - Nametag"), Description("Show nametag"), DefaultValue(true)]
        public SimplePropertyValue<Boolean> ShowNametag;
        [Auto, Category("-Configuration - Connector"), Description("Connector enabled and visible"), DefaultValue(true)]
        public SimplePropertyValue<Boolean> ConnectorEnabled;
        #endregion

        #region Events
        [Auto] void OnVisualAdded(Visual sender) {
            if (sender.Parent is SceneVisual) { Utilities.SnapToPanel(sender); }
        }

        [Auto] protected void OnReset(Visual sender) { Setup(sender); }

        private void Setup(Visual sender) {
            // update version
            Version.Value = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            // calculate result
            CalculateResult(sender, null, null);
            // update connector
            UpdateConnector(sender);
            // setup light power listeners
            for (int i = 1; i <= NumberOfLights.Value; i++) {
                var cp = sender.GetCustomProperty("R_Light" + i + "Power");
                if (cp != null) {
                    cp.UpdatedScript.NativeListeners -= CalculateResult;
                    cp.UpdatedScript.NativeListeners += CalculateResult;
                }
            }
        }

        [Auto] protected void OnShowNametagUpdated(Visual sender, Boolean value, Boolean oldValue) {
            var topPlate = sender.FindChild("TopPlate") as BoxVisual;
            if (topPlate != null) {
                topPlate.Visible = ShowNametag.Value;
            }
            var nametag = topPlate.FindImmediateChild("Nametag") as TextVisual;
            if (nametag != null) {
                nametag.Visible = ShowNametag.Value;
            }
        }

        [Auto] protected void OnNumberOfLightsUpdated(Visual sender, Int32 value, Int32 oldValue) {
            // error check number of lights entry
            var invalid = false;
            if(value < 1) {
                // invalid number of lights entry, overwrite value & continue
                invalid = true;
                value = 1;
            }
            if (value > oldValue) {
                // add additional lights
                for (int i = oldValue + 1; i <= value; i++) {
                    var existingLight = sender.FindImmediateChild("Light" + (i - 1)) as CylinderVisual;
                    var newLight = sender.FindImmediateChild("Light" + i);
                    var existingSeparator = sender.FindImmediateChild("Separator" + (i - 1)) as CylinderVisual;
                    var newSeparator = sender.FindImmediateChild("Separator" + i);
                    if (existingLight != null && newLight == null && existingSeparator != null && newSeparator == null) {
                        newLight = existingLight.Clone();
                        newLight.Parent = existingLight.Parent;
                        newLight.Location = existingLight.Location + vector(0, existingLight.Length + existingSeparator.Length, 0);
                        newLight.Name = "Light" + i;
                        newSeparator = existingSeparator.Clone();
                        newSeparator.Parent = existingSeparator.Parent;
                        newSeparator.Location = existingSeparator.Location + vector(0, existingLight.Length + existingSeparator.Length, 0);
                        newSeparator.Name = "Separator" + i;
                        if (!sender.HasCustomProperty("R_Light" + i + "Power")) {
                            var cp = sender.AddSimpleProperty("R_Light" + i + "Power", false, "Light" + i + " Power");
                            cp.Category = "-Wiring In";
                            cp.UpdatedScript.NativeListeners -= CalculateResult;
                            cp.UpdatedScript.NativeListeners += CalculateResult;
                            cp.PlaceOnLeftRail();
                        }
                        var nametag = newLight.FindImmediateChild("Nametag") as TextVisual;
                        if (nametag != null) {
                            nametag.Text = newLight.Name;
                        }
                    }
                }
            }
            else if (value < oldValue) {
                // remove extra lights
                for (int i = value + 1; i <= oldValue; i++) {
                    var lightToRemove = sender.FindImmediateChild("Light" + i);
                    if (lightToRemove != null) {
                        lightToRemove.Delete();
                    }
                    var separatorToRemove = sender.FindImmediateChild("Separator" + i);
                    if (separatorToRemove != null) {
                        separatorToRemove.Delete();
                    }
                    if (sender.HasCustomProperty("R_Light" + i + "Power")) {
                        var cp = sender.GetCustomProperty("R_Light" + i + "Power");
                        cp.RemoveFromWiringDiagram();
                        sender.RemoveCustomProperty(cp.Name);
                    }
                }
            }
            // calculate result
            CalculateResult(sender, null, null);
            sender.AutoSizeHeightInWiringDiagram();
            sender.CompressPropertiesOnRails();
            // refresh wiring diagram & properties grid
            WiringActions.RefreshWiringDiagram(document);
            Utilities.RefreshPropertiesGrid(sender);
            // update number of lights entry if it was invalid
            if (invalid) {
                NumberOfLights.Value = value;
            }
        }

        private void CalculateResult(Visual sender, object _value, object _oldValue) {
            // calculate light power values
            for (int i = 1; i <= NumberOfLights.Value; i++) {
                var light = sender.FindImmediateChild("Light" + i) as CylinderVisual;
                if (sender.HasCustomProperty("R_Light" + i + "Power") && light.HasCustomProperty("R_LightPower")) {
                    light.SetCustomPropertyValue("R_LightPower", (Boolean)sender.GetCustomPropertyValue("R_Light" + i + "Power"));
                }
            }
        }

        [Auto] protected void OnAfterPropertyUpdated(Visual sender, String name) {
            if (name == "Depth" || name == "Height" || name == "Width") {
                // update connector
                UpdateConnector(sender);
            }
        }

        [Auto] protected void OnConnectorEnabledUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateConnector(sender); }
        [Auto] protected void OnParentUpdated(Visual sender, Visual oldParent, Visual newParent) { UpdateConnector(sender); }
        #endregion

        #region Connector Configuration
        private void UpdateConnector(Visual visual) {
            // create panel connector on bottom
            BoxVisual sender = visual as BoxVisual;
            Demo3D.Visuals.Connector c = sender.FindCreateConnector("C1");
            c.Start = vector(0.0, -sender.Height / 2, 0.0);
            c.End = vector(-0.00001, -sender.Height / 2, 0.0);
            c.Normal = vector(0, -1, 0);
            c.Type = "PanelComponent";
            c.Allowed = new string[] { "Panel" };
            c.ReparentOnConnect = true;
            c.AlignmentStyle = ConnectorAlignmentStyle.Complete;
            c.MaxAllowedConnections = 1;
            c.AutoConnect = (ConnectorEnabled.Value) ? true : false;
            c.ControlPointEnabled = (ConnectorEnabled.Value) ? true : false;
            c.ControlPointSize = Math.Min(sender.Height, sender.Width) / 10;
            c.TextHeight = Math.Min(sender.Height, sender.Width) / 10;
            // set snap distance
            var xSpacing = sender.Width;
            var ySpacing = sender.Height;
            foreach (var ancestor in sender.Ancestors) {
                if (ancestor.Type.StartsWith("Panel") && ancestor.HasCustomProperty("XSpacing") && ancestor.HasCustomProperty("YSpacing")) {
                    xSpacing = ancestor.GetCustomPropertyValue("XSpacing") as DistanceProperty;
                    ySpacing = ancestor.GetCustomPropertyValue("YSpacing") as DistanceProperty;
                }
            }
            c.SnapDistance = Math.Min(xSpacing, ySpacing);
        }
        
        [Auto] protected void OnDragStart(Visual sender) {
            // disable show connectors while dragging (prevent red connector boxes showing)
            sender.UserVars["ShowConnectorsState"] = app.BuilderTool.ShowConnectors;
            app.BuilderTool.ShowConnectors = false;
        }

        [Auto] protected void OnDragEnd(Visual sender) {
            // restore show connectors state
            try { app.BuilderTool.ShowConnectors = (Boolean)sender.UserVars["ShowConnectorsState"]; }
            catch (Exception) { }
        }
        #endregion
    }
}