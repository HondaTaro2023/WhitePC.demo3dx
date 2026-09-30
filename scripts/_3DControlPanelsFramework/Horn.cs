using Demo3D.Common;
using Demo3D.Native;
using Demo3D.PLC.Comms;
using Demo3D.Utilities;
using Demo3D.Visuals;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Media;
using System.Reflection;

// Created by Mark Emeott
// Updated September 15, 2026

namespace Demo3D.Components._3DControlPanels {
    [Auto] public class Horn : NativeObject {
        public Horn(Visual sender) : base(sender) { Setup(sender); }

        #region Properties
        [Auto] CylinderVisual Cone;
        private UserResourceReference userResourceReference;
        private UserResource userResource;
        private SoundPlayer soundPlayer;

        [Auto, Category("-About"), ReadOnly(true), Description("Version")]
        protected SimplePropertyValue<String> Version;

        [Auto, Category("-Control - Horn"), AccessRights(AccessRights.ReadFromPLC), Description("Play the horn while true.")]
        public SimplePropertyValue<bool> PlayHorn;

        [Auto, Category("-Configuration - Horn"), Description("The horn sound to play (WAV format).")]
        public SimplePropertyValue<UserResourceReference> Sound;
        [Auto, Category("-Configuration - Horn"), Description("Horn on color")]
        public SimplePropertyValue<Color> OnColor;
        [Auto, Category("-Configuration - Horn"), Description("Horn off color")]
        public SimplePropertyValue<Color> OffColor;
        [Auto, Category("-Configuration - Horn"), Description("Calculate off color")]
        public SimplePropertyValue<Boolean> CalculateOffColor;
        [Auto, Category("-Configuration - Horn"), DefaultValue(true), Description("Whether to play the horn sound when the horn is on.")]
        public SimplePropertyValue<Boolean> SoundEnabled;

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
            // update connector
            UpdateConnector(sender);
            // stop horn playing
            PlayHorn.Value = false;
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

        [Auto] protected void OnPlayHornUpdated(Visual sender, bool value, bool oldValue) {
            if (PlayHorn.Value) {
                if (SoundEnabled.Value) {
                    // play horn
                    try {
                        // determine if a new sound player needs to be created (i.e. there's no sound player or the sound file changed)
                        if (soundPlayer == null || userResourceReference != Sound.Value) {
                            // delete existing sound player
                            soundPlayer?.Dispose();
                            // find new user resource for sound
                            userResourceReference = Sound.Value;
                            userResource = document.FindUserResource(Sound.Value);
                            if (userResource == null) {
                                Logger.Log("Error", $"{sender}'s Sound custom property value, '{Sound.Value}', wasn't found. Please browse for a valid Sound user resource.");
                                return;
                            }
                            // create new sound player (buffer the sound in memory so the player never reads a disposed stream)
                            var soundData = new MemoryStream();
                            using (var stream = userResource.GetInputStream(null)) {
                                stream.CopyTo(soundData);
                            }
                            soundData.Position = 0;
                            soundPlayer = new SoundPlayer(soundData);
                            // load synchronously so playback failures surface here rather than on the loader thread
                            soundPlayer.Load();
                            soundPlayer.PlayLooping();
                        }
                        else {
                            // re-use existing sound player
                            soundPlayer.PlayLooping();
                        }
                    }
                    catch (Exception ex) {
                        Logger.Log("Error", $"{sender}: error playing sound: {ex.Message}");
                    }
                }
            }
            else {
                // stop horn
                if (soundPlayer != null) {
                    soundPlayer.Stop();
                }
            }
            UpdateHornColor(sender);
        }

        [Auto] protected void OnAfterPropertyUpdated(Visual sender, String name) {
            if (name == "Depth" || name == "Height" || name == "Width") {
                // update connector
                UpdateConnector(sender);
            }
        }

        [Auto] protected void OnOnColorUpdated(Visual sender, Color value, Color oldValue) { UpdateColors(sender); }
        [Auto] protected void OnOffColorUpdated(Visual sender, Color value, Color oldValue) { UpdateColors(sender); }
        [Auto] protected void OnCalculateOffColorUpdated(Visual sender, Boolean value, Boolean oldValue) { UpdateColors(sender); }

        private void UpdateColors(Visual sender) {
            // calculate off color
            if (CalculateOffColor.Value) {
                double hue; double saturation; double value;
                Utilities.ColorToHSV(OnColor.Value, out hue, out saturation, out value);
                OffColor.Value = Utilities.ColorFromHSV(hue, saturation, value * 100 / 255);
            }
            // update light
            UpdateHornColor(sender);
            // refresh properties grid
            Utilities.RefreshPropertiesGrid(sender);
        }

        private void UpdateHornColor(Visual visual) {
            // update horn color
            Cone.Material.Color = PlayHorn.Value ? OnColor.Value : OffColor.Value;
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