using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PhaserMonitor.Model
{
    ///<summary>
    /// Page language 
    /// ENGLISH_E = 0, SPANISH_E = 1, PORTUGUESE_E =2
    /// 
    /// </summary>
    /// 
    /// <example>
    /// ENGLISH_E = 0, SPANISH_E = 1, PORTUGUESE_E =2
    /// </example>
    public enum Language { ENGLISH_E = 0, SPANISH_E, PORTUGUESE_E }
    public class PhaserConfigurationModel
    {

 

        
        [NotMapped]
        public PhaserNetworkConfigurationModel? Network { get; set; }

    

    public class BoardConfigurationModel
        {
            /// <summary>
            /// Size in chickens
            /// </summary>
            public int? Id { get; set; }
            
            public int? BatchEnd { get; set; }
            public int? BathSize { get; set; }
            public bool? ICASEnabled { get; set; }
            /// <summary>
            /// The equipment will start the stun once the power module is on.
            /// </summary>
            public bool? EarlyStun { get; set; }
            public int? CloseLotCount { get; set; }
            /// <summary>
            /// Distance in chickens
            /// </summary>
            public int? BathDistance { get; set; }

            public Language? Language { get; set; }
            public bool? HideCountAndSpeed { get; set; }

            public DateTime? Datetime { get; set; }
           // public bool? HideCountAndSpeed { get; set; }
        }
        public BoardConfigurationModel? Board { get; set; }
    }
}
