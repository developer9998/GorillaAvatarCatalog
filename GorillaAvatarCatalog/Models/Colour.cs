using Newtonsoft.Json;
using System;
using UnityEngine;

namespace GorillaAvatarCatalog.Models
{
    [Serializable]
    public class Colour
    {
        [JsonProperty(PropertyName = "r")]
        public int Red { get; set; }

        [JsonProperty(PropertyName = "g")]
        public int Green { get; set; }

        [JsonProperty(PropertyName = "b")]
        public int Blue { get; set; }

        public static implicit operator Color(Colour colour)
        {
            return new Color(colour.Red / 9f, colour.Green / 9f, colour.Blue / 9f);
        }

        public static implicit operator Colour(Color colour)
        {
            return new Colour
            {
                Red = Mathf.FloorToInt(colour.r * 9),
                Green = Mathf.FloorToInt(colour.g * 9),
                Blue = Mathf.FloorToInt(colour.b * 9)
            };
        }
    }
}
