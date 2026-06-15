using System;
using System.Collections.Generic;
using KRPC.Service;
using KRPC.Service.Attributes;
using UnityEngine;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// A ship under construction in the editor.
    /// </summary>
    [KRPCClass(GameScene = GameScene.Editor, Service = "SpaceCenter")]
    public class EditorShip
    {
        internal readonly ShipConstruct ship;

        internal EditorShip(ShipConstruct ship)
        {
            this.ship = ship;
        }

        /// <summary>
        /// Gets or sets the name of the ship.
        /// </summary>
        [KRPCProperty]
        public string ShipName
        {
            get { return ship.shipName; }
            set { ship.shipName = value; }
        }

        /// <summary>
        /// Gets or sets the ship description.
        /// </summary>
        [KRPCProperty]
        public string ShipDescription
        {
            get { return ship.shipDescription; }
            set { ship.shipDescription = value; }
        }

        /// <summary>
        /// Gets the current editor facility.
        /// </summary>
        [KRPCProperty]
        public EditorFacility EditorFacility
        {
            get
            {
                return (EditorFacility)ship.shipFacility;
            }
        }

        /// <summary>
        /// Returns an object which can be used to traverse and filter parts.
        /// </summary>
        [KRPCProperty]
        public Parts.EditorParts Parts
        {
            get { return new Parts.EditorParts(ship); }
        }
    }
}
