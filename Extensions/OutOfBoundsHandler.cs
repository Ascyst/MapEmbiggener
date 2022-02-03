using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MapEmbiggener.Extensions
{
    public class OutOfBoundsHandlerAdditionalData
    {
        public float DoTCounter;

        public OutOfBoundsHandlerAdditionalData()
        {
            this.DoTCounter = 0f;
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

