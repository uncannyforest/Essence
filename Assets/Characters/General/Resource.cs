using System;
using System.Collections.Generic;
using UnityEngine;

public class Resource : StatusQuantity {
    public string type = "Resource";

    override protected int? GetMaxFromStats(Stats stats) => stats.Res;

    public bool IsOut { get => Level <= 0; }

    public bool Has(int quantity = 1) => Level >= quantity;

    public bool Use(int quantity = 1) => Decrease(quantity);
}
