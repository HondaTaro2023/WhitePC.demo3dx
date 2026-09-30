using System;
using System.Drawing;
using System.Linq;
using Demo3D.Visuals;
using Microsoft.DirectX;

// Created by Mark Emeott
// Updated September 14, 2026

namespace Demo3D.Components._3DControlPanels {
    public class Utilities : NativeObject {
        // Color Utilities from StackOverflow
        public static void ColorToHSV(Color color, out double hue, out double saturation, out double value) {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            hue = color.GetHue();
            saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            value = max / 255d;
        }

        public static Color ColorFromHSV(double hue, double saturation, double value) {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(255, t, p, v);
            else
                return Color.FromArgb(255, v, p, q);
        }

        public static void RefreshPropertiesGrid(Visual sender) {
            // refresh properties grid (if visual is selected)
            if (app.Selection.Contains(sender)) {
                app.ForceRefreshPropertiesGrid();
            }
        }

        // Radius above half the cross-section makes BoxTubeVisual warn on every rebuild.
        public static void ClampCornerRadius(BoxTubeVisual tube) {
            if (tube == null) return;

            // Corners are named XminZmin..XmaxZmax, so Width and Depth form the cross-section.
            var limit = Math.Min(tube.Width, tube.Depth) / 2;
            if (tube.CornerRadius > limit) tube.CornerRadius = limit;
        }

        public static void SnapToPanel(Visual component) {
            // determine the selected feature & picked visual in the scene
            var selectedFeature = app.SelectionManager.SelectedFeature;
            var pickedVisual = selectedFeature?.ActualVisual;
            Connector panelConnector = null;
            // determine if picked visual is a panel connector
            if (pickedVisual is ConnectorControlPoint ccp && ccp.Connector.Type == "Panel") {
                if (!ccp.Connector.IsConnected) {
                    // picked visual's panel connector isn't connected, set panel connector to it
                    panelConnector = ccp.Connector;
                }
            }
            // if panel connector not set, look for the closest unconnected panel connector on the picked visual using its selected feature's world position
            if (panelConnector == null && pickedVisual != null && pickedVisual.AllConnectors.Any(c => c.Type == "Panel")) {
                var closest = pickedVisual.AllConnectors.Where(c => c.Type == "Panel").OrderBy(c => (c.ToVisualLocation().WorldLocation - selectedFeature.WorldPosition).Length()).FirstOrDefault();
                if (!closest.IsConnected) {
                    // closest connector available, set panel connector to it
                    panelConnector = closest;
                }
                else {
                    // closest connector not available, look for the next closest panel connector that isn't connected but has the same name prefix as closest connector (ignoring trailing numbers)
                    var startWith = closest.Name.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
                    var nextClosest = pickedVisual.AllConnectors.Where(c => c.Type == "Panel" && c.Name.StartsWith(startWith) && !c.IsConnected).OrderBy(c => (c.ToVisualLocation().WorldLocation - selectedFeature.WorldPosition).Length()).FirstOrDefault();
                    if (nextClosest != null && !nextClosest.IsConnected) {
                        panelConnector = nextClosest;
                    }
                }
            }
            // if panel connector found, connect the component visual's connector to it and move the component visual to align with the panel connector
            if (panelConnector != null) {
                // connect the component visual's connector to the panel connector
                var componentConnector = component.AllConnectors.Where(c => c.Type == "PanelComponent").First();
                componentConnector.ConnectTo(panelConnector, true);
                // determine where to place the component visual so that it aligns with the panel connector
                var panelConnectorWorld = panelConnector.ToVisualLocation().WorldLocation;
                var mountOffset = Vector3.TransformNormal(componentConnector.Start, component.WorldRotationMatrix);
                var adjustedWorld = panelConnectorWorld - mountOffset;
                // move the component visual to the adjusted world location using BeginInvoke to avoid potential threading issues
                app.BeginInvoke(() => component.WorldLocation = adjustedWorld);
            }
        }
    }
}