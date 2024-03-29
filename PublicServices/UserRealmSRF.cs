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
    public class UserRealmSRF : JsonSerialization<UserRealmSRF>
    {
        [DataMember]
        public int State { get; set; }
        [DataMember]
        public int UserState { get; set; }
        [DataMember]
        public string Login { get; set; }
        [DataMember]
        public string NameSpaceType { get; set; }
        [DataMember]
        public string DomainName { get; set; }
        [DataMember]
        public int FederationGlobalVersion { get; set; }
        [DataMember]
        public string AuthURL { get; set; }
        [DataMember]
        public string FederationBrandName { get; set; }
        [DataMember]
        public string CloudInstanceName { get; set; }
        [DataMember]
        public string CloudInstanceIssuerUri { get; set; }
    }
}
