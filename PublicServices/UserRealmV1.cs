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
    public class UserRealmV1 : JsonSerialization<UserRealmV1>
    {
        [DataMember]
        public string ver { get; set; }
        [DataMember]
        public string account_type { get; set; }
        [DataMember]
        public string domain_name { get; set; }
        [DataMember]
        public string federation_protocol { get; set; }
        [DataMember]
        public string federation_metadata_url { get; set; }
        [DataMember]
        public string federation_active_auth_url { get; set; }
        [DataMember]
        public string cloud_instance_name { get; set; }
        [DataMember]
        public string cloud_audience_urn { get; set; }
    }
}
