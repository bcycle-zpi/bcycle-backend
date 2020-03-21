﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace bcycle_backend.Models
{
    public class GroupTripPoint
    {
        public int Id { get; set; }
        public float Longitude { get; set; }
        public float Latitude { get; set; }
        public int Order { get; set; }
        public int GroupTripId { get; set; }
        
        public GroupTrip GroupTrip { get; set; }
    }
}
