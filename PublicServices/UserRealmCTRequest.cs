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
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PingCastleCloud.PublicServices
{
    [DataContractAttribute]
    public class UserRealmCTRequest : JsonSerialization<UserRealmCTRequest>
    {
        [DataMember]
        public string username { get; set; }
        [DataMember]
        public bool isOtherIdpSupported { get; set; }
        [DataMember]
        public bool checkPhones { get; set; }
        [DataMember]
        public bool isRemoteNGCSupported { get; set; }
        [DataMember]
        public bool isCookieBannerShown { get; set; }
        [DataMember]
        public bool isFidoSupported { get; set; }
        [DataMember]
        public string originalRequest { get; set; }
        [DataMember]
        public string flowToken { get; set; }
    }
}
