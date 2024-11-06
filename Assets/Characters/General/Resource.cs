using System;
using System.Collections.Generic;
using UnityEngine;

public class Resource : StatusQuantity {
    override protected void OnMaxChanged(Stats stats) => max = stats.Res;

    public bool Has(int quantity = 1) => Level >= quantity;

    public bool Use(int quantity = 1) => Decrease(quantity);
}
