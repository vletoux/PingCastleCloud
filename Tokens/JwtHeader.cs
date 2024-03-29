﻿//
// Copyright (c) Vincent LE TOUX for Ping Castle. All rights reserved.
// https://www.pingcastle.com
//
// Licensed under the Non-Profit OSL. See LICENSE file in the project root for full license information.
//
using PingCastleCloud.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PingCastleCloud.Tokens
{
    public class JwtHeader : JsonSerialization<JwtHeader>
    {
        public string alg { get; set; }
        public string typ { get; set; }
        public string x5t { get; set; }
    }
}
