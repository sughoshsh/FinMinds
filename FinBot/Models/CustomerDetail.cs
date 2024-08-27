using System;
using System.Collections.Generic;

namespace CoreBot.Models;

public partial class CustomerDetail
{
    public int Id { get; set; }

    public string CustCode { get; set; }

    public string CustStatus { get; set; }

    public int? CustLivePosition { get; set; }
}
