// From Kerbal Alarm Clock (https://github.com/TriggerAu/KerbalAlarmClock), released under
// the MIT license. Copyright (c) 2014, David Tregoning

#pragma warning disable 1591
#pragma warning disable 1572
#pragma warning disable 1573
#pragma warning disable 1587
#pragma warning disable 0414

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using KRPC.Utils;

namespace KRPC.KerbalAlarmClock
{

    ///////////////////////////////////////////////////////////////////////////////////////////
    // BELOW HERE SHOULD NOT BE EDITED - this links to the loaded KAC module without requiring a Hard Dependancy
    ///////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// The Wrapper class to access KAC from another plugin
    /// </summary>
    public class KACWrapper
    {
        protected static System.Type KACType;
        protected static System.Type KACAlarmType;

        protected static Object actualKAC = null;

        /// <summary>
        /// This is the Kerbal Alarm Clock object
        /// 
        /// SET AFTER INIT
        /// </summary>
        public static KACAPI KAC = null;
        /// <summary>
        /// Whether we found the KerbalAlarmClock assembly in the loadedassemblies. 
        /// 
        /// SET AFTER INIT
        /// </summary>
        public static Boolean AssemblyExists { get { return (KACType != null); } }
        /// <summary>
        /// Whether we managed to hook the running Instance from the assembly. 
        /// 
        /// SET AFTER INIT
        /// </summary>
        public static Boolean InstanceExists { get { return (KAC != null); } }
        /// <summary>
        /// Whether we managed to wrap all the methods/functions from the instance. 
        /// 
        /// SET AFTER INIT
        /// </summary>
        private static Boolean _KACWrapped = false;

        /// <summary>
        /// Whether the object has been wrapped and the APIReady flag is set in the real KAC
        /// </summary>
        public static Boolean APIReady { get { return _KACWrapped && KAC.APIReady && !NeedUpgrade; } }


        public static Boolean NeedUpgrade { get; private set; }

        /// <summary>
        /// This method will set up the KAC object and wrap all the methods/functions
        /// </summary>
        /// <param name="Force">This option will force the Init function to rebind everything</param>
        /// <returns></returns>
        public static Boolean InitKACWrapper()
        {
            //if (!_KACWrapped )
            //{
            //reset the internal objects
            _KACWrapped = false;
            actualKAC = null;
            KAC = null;
            LogFormatted("Attempting to Grab KAC Types...");

            //find the base type
            AssemblyLoader.loadedAssemblies.TypeOperation(t =>
            {
                if (t.FullName == "KerbalAlarmClock.KerbalAlarmClock")
                    KACType = t;
            });

            if (KACType == null)
            {
                return false;
            }

            LogFormatted("KAC Version:{0}", KACType.Assembly.GetName().Version.ToString());
            if (KACType.Assembly.GetName().Version.CompareTo(new System.Version(3, 0, 0, 5)) < 0)
            {
                //No TimeEntry or alarmchoice options = need a newer version
                NeedUpgrade = true;
            }
            
            //now the Alarm Type
            KACAlarmType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "KerbalAlarmClock.KACAlarm");

            // The alarm wrapper binds the mod's fields once and keeps them for the
            // assembly, so anything bound against the type of a previous run is dropped.
            KACAPI.KACAlarm.Unbind();

            if (KACAlarmType == null)
            {
                return false;
            }

            //now grab the running instance
            LogFormatted("Got Assembly Types, grabbing Instance");

            try {
                actualKAC = Accessor.StaticGetter<Object>(KACType, "APIInstance")();
            } catch (Exception) {
                NeedUpgrade = true;
                LogFormatted("No APIInstance found - most likely you have KAC v2 installed");
                //throw;
            }
            if (actualKAC == null)
            {
                LogFormatted("Failed grabbing Instance");
                return false;
            }

            //If we get this far we can set up the local object and its methods/functions
            LogFormatted("Got Instance, Creating Wrapper Objects");
            KAC = new KACAPI(actualKAC);
            //}
            _KACWrapped = true;
            return true;
        }

        /// <summary>
        /// The Type that is an analogue of the real KAC. This lets you access all the API-able properties and Methods of the KAC
        /// </summary>
        public class KACAPI
        {

            internal KACAPI(Object KAC)
            {
                //store the actual object
                actualKAC = KAC;

                //these sections get and store the reflection info and actual objects where required. Later in the properties we then read the values from the actual objects
                //for events we also add a handler
                LogFormatted("Getting APIReady Object");
                APIReadyField = Accessor.StaticGetter<Boolean>(KACType, "APIReady");
                LogFormatted("Success: " + (APIReadyField != null).ToString());

                //WORK OUT THE STUFF WE NEED TO HOOK FOR PEOPEL HERE
                LogFormatted("Getting Alarms Object");
                actualAlarms = Accessor.StaticGetter<Object>(KACType, "alarms")();
                LogFormatted("Success: " + (actualAlarms != null).ToString());

                //Events
                LogFormatted("Getting Alarm State Change Event");
                onAlarmStateChangedEvent = KACType.GetEvent("onAlarmStateChanged", BindingFlags.Public | BindingFlags.Instance);
                LogFormatted_DebugOnly("Success: " + (onAlarmStateChangedEvent != null).ToString());
                LogFormatted_DebugOnly("Adding Handler");
                AddHandler(onAlarmStateChangedEvent, actualKAC, AlarmStateChanged);

                //Methods
                LogFormatted("Getting Create Method");
                CreateAlarmMethod = Accessor.Method<Func<Object, AlarmTypeEnum, String, Double, String>>(KACType, "CreateAlarm");
                LogFormatted_DebugOnly("Success: " + (CreateAlarmMethod != null).ToString());

                LogFormatted("Getting Delete Method");
                DeleteAlarmMethod = Accessor.Method<Func<Object, String, Boolean>>(KACType, "DeleteAlarm");
                LogFormatted_DebugOnly("Success: " + (DeleteAlarmMethod != null).ToString());

                LogFormatted("Getting DrawAlarmAction");
                DrawAlarmActionChoiceMethod = Accessor.Method<Func<Object, Int32, String, Int32, Int32, Int32>>(KACType, "DrawAlarmActionChoiceAPI");
                LogFormatted_DebugOnly("Success: " + (DrawAlarmActionChoiceMethod != null).ToString());

                //LogFormatted("Getting DrawTimeEntry");
                //DrawTimeEntryMethod = KACType.GetMethod("DrawTimeEntryAPI", BindingFlags.Public | BindingFlags.Instance);
                //LogFormatted_DebugOnly("Success: " + (DrawTimeEntryMethod != null).ToString());

				//Commenting out rubbish lines
                //MethodInfo[] mis = KACType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                //foreach (MethodInfo mi in mis)
                //{
                //    LogFormatted("M:{0}-{1}", mi.Name, mi.DeclaringType);
                //}
            }

            private Object actualKAC;

            private Func<Boolean> APIReadyField;
            /// <summary>
            /// Whether the APIReady flag is set in the real KAC
            /// </summary>
            public Boolean APIReady
            {
                get
                {
                    if (APIReadyField == null)
                        return false;

                    return APIReadyField();
                }
            }

            #region Alarms
            private Object actualAlarms;

            /// <summary>
            /// The list of Alarms that are currently active in game
            /// </summary>
            internal KACAlarmList Alarms
            {
                get
                {
                    return ExtractAlarmList(actualAlarms);
                }
            }

            /// <summary>
            /// This converts the KACAlarmList actual object to a new List for consumption
            /// </summary>
            /// <param name="actualAlarmList"></param>
            /// <returns></returns>
            private KACAlarmList ExtractAlarmList(Object actualAlarmList)
            {
                KACAlarmList ListToReturn = new KACAlarmList();
                try
                {
                    //iterate each "value" in the dictionary

                    foreach (var item in (IList)actualAlarmList)
                    {
                        KACAlarm r1 = new KACAlarm(item);
                        ListToReturn.Add(r1);
                    }
                }
                catch (Exception)
                {
                    //LogFormatted("Arrggg: {0}", ex.Message);
                    //throw ex;
                    //
                }
                return ListToReturn;
            }

            #endregion

            #region Events
            /// <summary>
            /// Takes an EventInfo and binds a method to the event firing
            /// </summary>
            /// <param name="Event">EventInfo of the event we want to attach to</param>
            /// <param name="KACObject">actual object the eventinfo is gathered from</param>
            /// <param name="Handler">Method that we are going to hook to the event</param>
            protected void AddHandler(EventInfo Event, Object KACObject, Action<Object> Handler)
            {
                //build a delegate
                Delegate d = Delegate.CreateDelegate(Event.EventHandlerType, Handler.Target, Handler.Method);
                //get the Events Add method
                MethodInfo addHandler = Event.GetAddMethod();
                //and add the delegate
                addHandler.Invoke(KACObject, new System.Object[] { d });
            }

            //the info about the event;
            private EventInfo onAlarmStateChangedEvent;

            /// <summary>
            /// Event that fires when the State of an Alarm changes
            /// </summary>
            public event AlarmStateChangedHandler onAlarmStateChanged;
            /// <summary>
            /// Structure of the event delegeate
            /// </summary>
            /// <param name="e"></param>
            public delegate void AlarmStateChangedHandler(AlarmStateChangedEventArgs e);
            /// <summary>
            /// This is the structure that holds the event arguments
            /// </summary>
            public class AlarmStateChangedEventArgs
            {
                public AlarmStateChangedEventArgs(System.Object actualEvent, KACAPI kac)
                {
                    Type type = actualEvent.GetType();
                    if (type != boundEventType)
                    {
                        alarmOfEvent = Accessor.Getter<Object, Object>(type, "alarm");
                        typeOfEvent = Accessor.Getter<Object, KACAlarm.AlarmStateEventsEnum>(type, "eventType");
                        boundEventType = type;
                    }
                    this.alarm = new KACAlarm(alarmOfEvent(actualEvent));
                    this.eventType = typeOfEvent(actualEvent);
                }

                // Bound for the mod's event type, which is the same for every event
                private static Type boundEventType;
                private static Func<Object, Object> alarmOfEvent;
                private static Func<Object, KACAlarm.AlarmStateEventsEnum> typeOfEvent;

                /// <summary>
                /// Alarm that has had the state change
                /// </summary>
                public KACAlarm alarm;
                /// <summary>
                /// What the state was before the event
                /// </summary>
                public KACAlarm.AlarmStateEventsEnum eventType;
            }


            /// <summary>
            /// private function that grabs the actual event and fires our wrapped one
            /// </summary>
            /// <param name="actualEvent">actual event from the KAC</param>
            private void AlarmStateChanged(object actualEvent)
            {
                if (onAlarmStateChanged != null)
                {
                    onAlarmStateChanged(new AlarmStateChangedEventArgs(actualEvent, this));
                }
            }
            #endregion


            #region Methods
            private Func<Object, AlarmTypeEnum, String, Double, String> CreateAlarmMethod;

            /// <summary>
            /// Create a new Alarm
            /// </summary>
            /// <param name="AlarmType">What type of alarm are we creating</param>
            /// <param name="Name">Name of the Alarm for the display</param>
            /// <param name="UT">Universal Time for the alarm</param>
            /// <returns>ID of the newly created alarm</returns>
            internal String CreateAlarm(AlarmTypeEnum AlarmType, String Name, Double UT)
            {
                return CreateAlarmMethod(actualKAC, AlarmType, Name, UT);
            }


            private Func<Object, String, Boolean> DeleteAlarmMethod;
            /// <summary>
            /// Delete an Alarm
            /// </summary>
            /// <param name="AlarmID">Unique ID of the alarm</param>
            /// <returns>Success of the deletion</returns>
            internal Boolean DeleteAlarm(String AlarmID)
            {
                return DeleteAlarmMethod(actualKAC, AlarmID);
            }


            private Func<Object, Int32, String, Int32, Int32, Int32> DrawAlarmActionChoiceMethod;
            /// <summary>
            /// Delete an Alarm
            /// </summary>
            /// <param name="AlarmID">Unique ID of the alarm</param>
            /// <returns>Success of the deletion</returns>
            internal Boolean DrawAlarmActionChoice(ref AlarmActionEnum Choice, String LabelText, Int32 LabelWidth, Int32 ButtonWidth)
            {
                Int32 InValue = (Int32)Choice;
                Int32 OutValue = DrawAlarmActionChoiceMethod(actualKAC, InValue, LabelText, LabelWidth, ButtonWidth);

                Choice = (AlarmActionEnum)OutValue;
                return (InValue != OutValue);
            }

            //Remmed out due to it borking window layout
            //private MethodInfo DrawTimeEntryMethod;
            ///// <summary>
            ///// Delete an Alarm
            ///// </summary>
            ///// <param name="AlarmID">Unique ID of the alarm</param>
            ///// <returns>Success of the deletion</returns>

            //internal Boolean DrawTimeEntry(ref Double Time, TimeEntryPrecisionEnum Prec, String LabelText, Int32 LabelWidth)
            //{
            //    Double InValue = Time;
            //    Double OutValue = (Double)DrawTimeEntryMethod.Invoke(actualKAC, new System.Object[] { InValue, (Int32)Prec, LabelText, LabelWidth });

            //    Time = OutValue;
            //    return (InValue != OutValue);
            //}


            #endregion

            public class KACAlarm
            {
                internal KACAlarm(Object a)
                {
                    actualAlarm = a;
                    EnsureBound();
                }

                // The lookups below depend on the mod's alarm type alone, so they are made
                // once for the assembly rather than for each alarm wrapped. An alarm is
                // wrapped every time the alarms are listed, and there are twenty of them.
                private static bool bound;

                internal static void Unbind()
                {
                    bound = false;
                }

                private static void EnsureBound()
                {
                    if (bound)
                        return;
                    Bind();
                    bound = true;
                }

                private static void Bind()
                {
                    VesselIDField = Accessor.Field<Object, String>(KACAlarmType, "VesselID");
                    IDField = Accessor.Getter<Object, String>(KACAlarmType, "ID");
                    NameField = Accessor.Field<Object, String>(KACAlarmType, "Name");
                    NotesField = Accessor.Field<Object, String>(KACAlarmType, "Notes");
                    AlarmTypeField = Accessor.Getter<Object, AlarmTypeEnum>(KACAlarmType, "TypeOfAlarm");
                    AlarmTimeProperty = Accessor.Field<Object, Double>(KACAlarmType, "AlarmTimeUT");
                    AlarmMarginField = Accessor.Field<Object, Double>(KACAlarmType, "AlarmMarginSecs");
                    RemainingField = Accessor.Getter<Object, Double>(KACAlarmType, "Remaining");

                    ActionActionProperty = Accessor.Field<Object, AlarmActionEnum>(KACAlarmType, "AlarmActionConvert");

                    XferOriginBodyNameField = Accessor.Field<Object, String>(KACAlarmType, "XferOriginBodyName");
                    //LogFormatted("XFEROrigin:{0}", XferOriginBodyNameField == null);
                    XferTargetBodyNameField = Accessor.Field<Object, String>(KACAlarmType, "XferTargetBodyName");

                    RepeatAlarmField = Accessor.Field<Object, Boolean>(KACAlarmType, "RepeatAlarm");
                    RepeatAlarmPeriodProperty = Accessor.Field<Object, Double>(KACAlarmType, "RepeatAlarmPeriodUT");
                    SupportsRepeatProperty = Accessor.Getter<Object, Boolean>(KACAlarmType, "SupportsRepeat");
                    SupportsRepeatPeriodProperty = Accessor.Getter<Object, Boolean>(KACAlarmType, "SupportsRepeatPeriod");

                    EnabledField = Accessor.Field<Object, Boolean>(KACAlarmType, "Enabled");
                    PlaySoundField = Accessor.Field<Object, Boolean>(KACAlarmType, "PlaySound");
                    TriggeredField = Accessor.Getter<Object, Boolean>(KACAlarmType, "Triggered");

                    //PropertyInfo[] pis = KACAlarmType.GetProperties();
                    //foreach (PropertyInfo pi in pis)
                    //{
                    //    LogFormatted("P:{0}-{1}", pi.Name, pi.DeclaringType);
                    //}
                    //FieldInfo[] fis = KACAlarmType.GetFields();
                    //foreach (FieldInfo fi in fis)
                    //{
                    //    LogFormatted("F:{0}-{1}", fi.Name, fi.DeclaringType);
                    //}
                }
                private Object actualAlarm;

                private static Field<Object, String> VesselIDField;
                /// <summary>
                /// Unique Identifier of the Vessel that the alarm is attached to
                /// </summary>
                public String VesselID
                {
                    get { return VesselIDField.Get(actualAlarm); }
                    set { VesselIDField.Set(actualAlarm, value); }
                }

                private static Func<Object, String> IDField;
                /// <summary>
                /// Unique Identifier of this alarm
                /// </summary>
                public String ID
                {
                    get { return IDField(actualAlarm); }
                }

                private static Field<Object, String> NameField;
                /// <summary>
                /// Short Text Name for the Alarm
                /// </summary>
                public String Name
                {
                    get { return NameField.Get(actualAlarm); }
                    set { NameField.Set(actualAlarm, value); }
                }

                private static Field<Object, String> NotesField;
                /// <summary>
                /// Longer Text Description for the Alarm
                /// </summary>
                public String Notes
                {
                    get { return NotesField.Get(actualAlarm); }
                    set { NotesField.Set(actualAlarm, value); }
                }

                private static Field<Object, String> XferOriginBodyNameField;
                /// <summary>
                /// Name of the origin body for a transfer
                /// </summary>
                public String XferOriginBodyName
                {
                    get { return XferOriginBodyNameField.Get(actualAlarm); }
                    set { XferOriginBodyNameField.Set(actualAlarm, value); }
                }

                private static Field<Object, String> XferTargetBodyNameField;
                /// <summary>
                /// Name of the destination body for a transfer
                /// </summary>
                public String XferTargetBodyName
                {
                    get { return XferTargetBodyNameField.Get(actualAlarm); }
                    set { XferTargetBodyNameField.Set(actualAlarm, value); }
                }
                
                private static Func<Object, AlarmTypeEnum> AlarmTypeField;
                /// <summary>
                /// What type of Alarm is this - affects icon displayed and some calc options
                /// </summary>
                public AlarmTypeEnum AlarmType { get { return AlarmTypeField(actualAlarm); } }

                private static Field<Object, Double> AlarmTimeProperty;
                /// <summary>
                /// In game UT value of the alarm
                /// </summary>
                public Double AlarmTime
                {
                    get { return AlarmTimeProperty.Get(actualAlarm); }
                    set { AlarmTimeProperty.Set(actualAlarm, value); }
                }

                private static Field<Object, Double> AlarmMarginField;
                /// <summary>
                /// In game seconds the alarm will fire before the event it is for
                /// </summary>
                public Double AlarmMargin
                {
                    get { return AlarmMarginField.Get(actualAlarm); }
                    set { AlarmMarginField.Set(actualAlarm, value); }
                }

                /// <summary>
                /// What should the Alarm Clock do when the alarm fires
                /// </summary>
                public AlarmActionEnum AlarmAction
                {
                    get { return ActionActionProperty.Get(actualAlarm); }
                    set { ActionActionProperty.Set(actualAlarm, value); }
                }
                private static Field<Object, AlarmActionEnum> ActionActionProperty;



                private static Func<Object, Double> RemainingField;
                /// <summary>
                /// How much Game time is left before the alarm fires
                /// </summary>
                public Double Remaining { get { return RemainingField(actualAlarm); } }


                private static Field<Object, Boolean> RepeatAlarmField;
                /// <summary>
                /// Whether the alarm will be repeated after it fires
                /// </summary>
                public Boolean RepeatAlarm
                {
                    get { return RepeatAlarmField.Get(actualAlarm); }
                    set { RepeatAlarmField.Set(actualAlarm, value); }
                }
                private static Field<Object, Double> RepeatAlarmPeriodProperty;
                /// <summary>
                /// Value in Seconds after which the alarm will repeat
                /// </summary>
                public Double RepeatAlarmPeriod
                {
                    get
                    {
                        try { return RepeatAlarmPeriodProperty.Get(actualAlarm); }
                        catch (Exception) { return 0; }
                    }
                    set { RepeatAlarmPeriodProperty.Set(actualAlarm, value); }
                }
                private static Func<Object, Boolean> SupportsRepeatProperty;
                /// <summary>
                /// Whether this alarm's type supports repeating
                /// </summary>
                public Boolean SupportsRepeat
                {
                    get { return SupportsRepeatProperty(actualAlarm); }
                }
                private static Func<Object, Boolean> SupportsRepeatPeriodProperty;
                /// <summary>
                /// Whether this alarm's type supports a repeat period
                /// </summary>
                public Boolean SupportsRepeatPeriod
                {
                    get { return SupportsRepeatPeriodProperty(actualAlarm); }
                }
                private static Field<Object, Boolean> EnabledField;
                /// <summary>
                /// Whether the alarm is enabled
                /// </summary>
                public Boolean Enabled
                {
                    get { return EnabledField.Get(actualAlarm); }
                    set { EnabledField.Set(actualAlarm, value); }
                }
                private static Field<Object, Boolean> PlaySoundField;
                /// <summary>
                /// Whether the alarm plays a sound when it fires
                /// </summary>
                public Boolean PlaySound
                {
                    get { return PlaySoundField.Get(actualAlarm); }
                    set { PlaySoundField.Set(actualAlarm, value); }
                }
                private static Func<Object, Boolean> TriggeredField;
                /// <summary>
                /// Whether the alarm has been triggered (the field is internal to the mod)
                /// </summary>
                public Boolean Triggered
                {
                    get { return TriggeredField(actualAlarm); }
                }

                public enum AlarmStateEventsEnum
                {
                    Created,
                    Triggered,
                    Closed,
                    Deleted,
                }
            }

            public enum AlarmTypeEnum
            {
                Raw,
                Maneuver,
                ManeuverAuto,
                Apoapsis,
                Periapsis,
                AscendingNode,
                DescendingNode,
                LaunchRendevous,
                Closest,
                SOIChange,
                SOIChangeAuto,
                Transfer,
                TransferModelled,
                Distance,
                Crew,
                EarthTime,
                Contract,
                ContractAuto,
                ScienceLab
            }

            public enum AlarmActionEnum
            {
                [Description("Do Nothing-Delete When Past")]        DoNothingDeleteWhenPassed,
                [Description("Do Nothing")]                         DoNothing,
                [Description("Message Only-No Affect on warp")]     MessageOnly,
                [Description("Kill Warp Only-No Message")]          KillWarpOnly,
                [Description("Kill Warp and Message")]              KillWarp,
                [Description("Pause Game and Message")]             PauseGame,
                [Description("Custom Config")]                      Custom,
                [Description("Converted to Components")]            Converted
            }

            public enum TimeEntryPrecisionEnum
            {
                Seconds = 0,
                Minutes = 1,
                Hours = 2,
                Days = 3,
                Years = 4
            }

            public class KACAlarmList : List<KACAlarm>
            {

            }
        }
        #region Logging Stuff
        /// <summary>
        /// Some Structured logging to the debug file - ONLY RUNS WHEN DLL COMPILED IN DEBUG MODE
        /// </summary>
        /// <param name="Message">Text to be printed - can be formatted as per String.format</param>
        /// <param name="strParams">Objects to feed into a String.format</param>
        [System.Diagnostics.Conditional("DEBUG")]
        internal static void LogFormatted_DebugOnly(String Message, params Object[] strParams)
        {
            LogFormatted(Message, strParams);
        }

        /// <summary>
        /// Some Structured logging to the debug file
        /// </summary>
        /// <param name="Message">Text to be printed - can be formatted as per String.format</param>
        /// <param name="strParams">Objects to feed into a String.format</param>
        internal static void LogFormatted(String Message, params Object[] strParams)
        {
            Message = String.Format(CultureInfo.InvariantCulture, Message, strParams);
            String strMessageLine = String.Format(CultureInfo.InvariantCulture, "{0},{2}-{3},{1}",
                DateTime.Now, Message, System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
            UnityEngine.Debug.Log(strMessageLine);
        }
        #endregion
    }
}
