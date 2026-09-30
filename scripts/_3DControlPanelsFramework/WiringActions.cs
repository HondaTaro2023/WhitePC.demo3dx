#nullable enable

using System;
using System.Reflection;
using Demo3D.Visuals;

namespace Demo3D.WiringDiagram {
    static class WiringActions {
        class WiringActionsMethods {
            internal Assembly? Assembly { get; }
            internal MethodInfo? PlaceOnLeftRail_Bdib { get; }
            internal MethodInfo? PlaceOnLeftRail_Bib { get; }
            internal MethodInfo? PlaceOnRightRail_Bdib { get; }
            internal MethodInfo? PlaceOnRightRail_Bib { get; }
            internal MethodInfo? RemoveFromWiringDiagram { get; }
            internal MethodInfo? AutoSizeHeightInWiringDiagram { get; }
            internal MethodInfo? CompressPropertiesOnRails { get; }
            internal MethodInfo? RefreshWiringDiagram { get; }

            internal WiringActionsMethods(Assembly? assembly) {
                this.Assembly = assembly;

                try {
                    var wiringActions = assembly?.GetType("Demo3D.WiringDiagram.WiringActions");

                    if (wiringActions is not null) {
                        this.PlaceOnLeftRail_Bdib = wiringActions.GetMethod("PlaceOnLeftRail", [typeof(BindableItem), typeof(double), typeof(int), typeof(bool)]);
                        this.PlaceOnLeftRail_Bib = wiringActions.GetMethod("PlaceOnLeftRail", [typeof(BindableItem), typeof(int), typeof(bool)]);
                        this.PlaceOnRightRail_Bdib = wiringActions.GetMethod("PlaceOnRightRail", [typeof(BindableItem), typeof(double), typeof(int), typeof(bool)]);
                        this.PlaceOnRightRail_Bib = wiringActions.GetMethod("PlaceOnRightRail", [typeof(BindableItem), typeof(int), typeof(bool)]);
                        this.RemoveFromWiringDiagram = wiringActions.GetMethod("RemoveFromWiringDiagram", [typeof(BindableItem), typeof(int)]);
                        this.AutoSizeHeightInWiringDiagram = wiringActions.GetMethod("AutoSizeHeightInWiringDiagram", [typeof(Visual)]);
                        this.CompressPropertiesOnRails = wiringActions.GetMethod("CompressPropertiesOnRails", [typeof(Visual), typeof(double)]);
                        this.RefreshWiringDiagram = wiringActions.GetMethod("RefreshWiringDiagram", [typeof(Document)]);
                    }
                }
                catch { }
            }
        }

        static readonly object wiringLock = new();
        static bool subscribed;
        static WiringActionsMethods? wiringActions;

        static WiringActionsMethods GetWiringActions() {
            lock (wiringLock) {
                if (wiringActions is not null) return wiringActions;

                Assembly? assembly = null;

                try { assembly = Assembly.Load("Wiring"); }
                catch { }

                if (!subscribed) {
                    AppDomain.CurrentDomain.AssemblyLoad += (s, a) => wiringActions = null;
                    subscribed = true;
                }

                if (wiringActions is null || !ReferenceEquals(wiringActions.Assembly, assembly)) {
                    wiringActions = new WiringActionsMethods(assembly);
                }

                return wiringActions;
            }
        }

        public static void PlaceOnLeftRail(this BindableItem property, double locationY, int instance = 0, bool mirror = false) {
            GetWiringActions().PlaceOnLeftRail_Bdib?.Invoke(null, new object[] { property, locationY, instance, mirror });
        }

        public static void PlaceOnLeftRail(this BindableItem property, int instance = 0, bool mirror = false) {
            GetWiringActions().PlaceOnLeftRail_Bib?.Invoke(null, new object[] { property, instance, mirror });
        }

        public static void PlaceOnRightRail(this BindableItem property, double locationY, int instance = 0, bool mirror = false) {
            GetWiringActions().PlaceOnRightRail_Bdib?.Invoke(null, new object[] { property, locationY, instance, mirror });
        }

        public static void PlaceOnRightRail(this BindableItem property, int instance = 0, bool mirror = false) {
            GetWiringActions().PlaceOnRightRail_Bib?.Invoke(null, new object[] { property, instance, mirror });
        }

        public static void RemoveFromWiringDiagram(this BindableItem property, int instance = 0) {
            GetWiringActions().RemoveFromWiringDiagram?.Invoke(null, new object[] { property, instance });
        }

        public static void AutoSizeHeightInWiringDiagram(this Visual visual) {
            GetWiringActions().AutoSizeHeightInWiringDiagram?.Invoke(null, new object[] { visual });
        }

        public static void CompressPropertiesOnRails(this Visual visual, double separation = 20.0) {
            GetWiringActions().CompressPropertiesOnRails?.Invoke(null, new object[] { visual, separation });
        }

        public static void RefreshWiringDiagram(this Document document) {
            GetWiringActions().RefreshWiringDiagram?.Invoke(null, new object[] { document });
        }
    }
}