using System;
using System.Collections.Generic;

namespace CoreBot.Models;

public partial class Eodpublisher
{
    public int Id { get; set; }

    public string SystemName { get; set; }

    public string PubStatus { get; set; }

    public DateOnly? EodDate { get; set; }
}
