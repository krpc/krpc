using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// An Alarm. Can be accessed using <see cref="SpaceCenter.AlarmClock"/>.
    /// </summary>
    [KRPCClass(Service = "SpaceCenter")]
    public class Alarm : Equatable<Alarm>
    {
        /// <summary>
        /// Create a alarm object from a KSP alarmTypeBase
        /// </summary>
        public Alarm(AlarmTypeBase alarm)
        {
            InternalAlarm = alarm;
        }

        /// <summary>
        /// The KSP Alarm
        /// </summary>
        public AlarmTypeBase InternalAlarm { get; private set; }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(Alarm other)
        {
            return !ReferenceEquals(other, null) && InternalAlarm.Id == other.InternalAlarm.Id; //Interna.ContractID == other.InternalContract.ContractID;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode()
        {
            return InternalAlarm.GetHashCode();
        }

        /// <summary>
        /// Type of Alarm
        /// </summary>
        [KRPCProperty]
        public string Type
        {
            get { UdateAlarm(); return InternalAlarm.TypeName; }
        }

        /// <summary>
        /// Title of the Alarm
        /// </summary>
        [KRPCProperty]
        public string Title
        {
            get { UdateAlarm(); return InternalAlarm.title; }


        }

        /// <summary>
        /// Description of the contract.
        /// </summary>
        [KRPCProperty]
        public string Description
        {
            get { UdateAlarm(); return InternalAlarm.description; }
        }

        /// <summary>
        /// Notes for the contract.
        /// </summary>
        [KRPCProperty]
        public double UT
        {
            get { UdateAlarm(); return InternalAlarm.ut; }
        }

        /// <summary>
        /// Synopsis for the contract.
        /// </summary>
        [KRPCProperty]
        public double TimeTill
        {
            get { UdateAlarm(); return InternalAlarm.TimeToAlarm; }
        }

        /// <summary>
        /// Synopsis for the contract.
        /// </summary>
        [KRPCProperty]
        public double EventOffset
        {
            get { UdateAlarm(); return InternalAlarm.eventOffset; }
        }

        /// <summary>
        /// Synopsis for the contract.
        /// </summary>
        [KRPCProperty(Nullable = true)]
        public Vessel Vessel
        {
            get
            {
                UdateAlarm();
                KRPC.SpaceCenter.Services.Vessel V = new Vessel(InternalAlarm.Vessel);


                return V;
            }
        }


        /// <summary>
        /// Synopsis for the contract.
        /// </summary>
        [KRPCProperty(Nullable = true)]
        public int ID
        {
            get
            {
                UdateAlarm();
                return (int)InternalAlarm.Id;
            }
        }

        public void UdateAlarm() 
        {
            AlarmTypeBase newalarm;
            AlarmClockScenario.TryGetAlarm(InternalAlarm.Id, out newalarm);
            InternalAlarm = newalarm;
        }
       
    }
}
