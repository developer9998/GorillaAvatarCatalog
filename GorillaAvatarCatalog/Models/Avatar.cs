using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using static GorillaNetworking.CosmeticsController;

namespace GorillaAvatarCatalog.Models
{
    [Serializable]
    public class Avatar
    {
        [JsonProperty(PropertyName = "username")]
        public string PlayerName { get; set; }

        [JsonProperty(PropertyName = "colour")]
        public Colour PlayerColour { get; set; }

        [JsonProperty(PropertyName = "cosmetics")]
        public Dictionary<CosmeticSlots, string> Cosmetics { get; set; }

        [JsonIgnore]
        public bool IsValid
        {
            get
            {
                string username = PlayerName;
                if (string.IsNullOrEmpty(username) || string.IsNullOrWhiteSpace(username) || username.Length > 12)
                    return false;

                Colour colour = PlayerColour;
                if (colour.Red < 0 || colour.Red > 9 || colour.Green < 0 || colour.Green > 9 || colour.Blue < 0 || colour.Blue > 9)
                    return false;

                Dictionary<CosmeticSlots, string> cosmetics = Cosmetics;
                if (cosmetics == null || cosmetics.Count == 0 || cosmetics.Values.All(itemId => instance.GetItemNameFromDisplayName(itemId) == "null"))
                    return false;

                return true;
            }
        }
    }
}
