﻿using System.Runtime.Serialization;

namespace GoG.Infrastructure.Engine
{
    public class GoPlayer
    {
        public string Name { get; set; }

        public PlayerType PlayerType { get; set; }

        public decimal Score { get; set; }

        public int Level { get; set; }
    }


}