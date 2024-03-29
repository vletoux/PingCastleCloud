﻿//
// Copyright (c) Vincent LE TOUX for Ping Castle. All rights reserved.
// https://www.pingcastle.com
//
// Licensed under the Non-Profit OSL. See LICENSE file in the project root for full license information.
//
using PingCastleCloud.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PingCastleCloud.Rules
{
    [RuleModel("GuestUserAccessRestriction1")]
    [RuleComputation(RuleComputationType.TriggerOnPresence, 25)]
    [RuleMaturityLevel(1)]
    public class GuestUserAccessRestriction1 : RuleBase
    {
        protected override int? AnalyzeDataNew(HealthCheckCloudData healthCheckCloudData)
        {
            if (string.Equals(healthCheckCloudData.PolicyGuestUserRoleId, "a0b1b346-4d3e-4e8b-98f8-753987be4970", StringComparison.OrdinalIgnoreCase))
            {
                AddRawDetail(healthCheckCloudData.PolicyGuestUserRoleId);
            }
            return null;
        }

    }
}
