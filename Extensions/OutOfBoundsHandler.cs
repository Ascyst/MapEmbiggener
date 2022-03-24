using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MapEmbiggener.Extensions
{
    public class OutOfBoundsHandlerAdditionalData
    {
        /// <summary>
        /// time since the last damage was applied
        /// </summary>
        public float DoTCounter;
        /// <summary>
        /// amount of time the player has been out of bounds
        /// </summary>
        public float OOBTimer;
        /// <summary>
        /// number of damage ticks that have been applied since the last time the player was in bounds
        /// </summary>
        public int DamageTicks;

        public OutOfBoundsHandlerAdditionalData()
        {
            this.DoTCounter = 0f;
            this.OOBTimer = 0f;
            this.DamageTicks = 0;
        }
    }
    public static class OutOfBoundsHandlerExtensions
    {
        public static readonly ConditionalWeakTable<OutOfBoundsHandler, OutOfBoundsHandlerAdditionalData> data =
            new ConditionalWeakTable<OutOfBoundsHandler, OutOfBoundsHandlerAdditionalData>();

        public static OutOfBoundsHandlerAdditionalData GetAdditionalData(this OutOfBoundsHandler outOfBoundsHandler)
        {
            return data.GetOrCreateValue(outOfBoundsHandler);
        }

        public static void AddData(this OutOfBoundsHandler outOfBoundsHandler, OutOfBoundsHandlerAdditionalData value)
        {
            try
            {
                data.Add(outOfBoundsHandler, value);
            }
            catch (Exception) { }
        }
    }
}

