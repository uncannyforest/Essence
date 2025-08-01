using System;
using UnityEngine;

// Example usage:
//
// WhyNot isValid =
//     characterFocus == null ? "null_focus" :                 // string
//     resource?.Has() == false ? "insufficient_resource" :    // string
//     general.hasAttack ? Will.IsThreat() :                   // WhyNot (maybe true, maybe string)
//     (WhyNot)true;                                           // if none of the above: true
// 
// if (isValid.NegLog("not valid because"))                    // logs reason if false; always returns bool
public class WhyNot {
    private string reason;

    private WhyNot(string reason) => this.reason = reason;

    public static implicit operator WhyNot(string reason) => new WhyNot(reason);
    public static implicit operator WhyNot(bool ok) {
        if (!ok) throw new ArgumentException("Cannot cast false to WhyNot - must provide reason");
        return new WhyNot(null);
    } 

    override public string ToString() => (string)this;
    public static explicit operator string(WhyNot whyNot) => whyNot.reason;       // might return null
    public static explicit operator bool(WhyNot whyNot) => whyNot.reason == null; // explicitly specify we don't want to log

    public bool NegLog(string prefix) {
        if (reason != null)
            Debug.Log(prefix + ": " + reason);
        return (bool)this;
    }

    public static bool operator true(WhyNot wn) => (bool)wn;
    public static bool operator false(WhyNot wn) => !(bool)wn;
    public static bool operator !(WhyNot wn) => !(bool)wn;
    public static WhyNot operator &(WhyNot a, WhyNot b) {
        if ((bool)a) {
            if ((bool)b) return true;
            else return b;
        } else {
            if ((bool)b) return a;
            else return a + "+" + b;
        }
    }
    public static WhyNot operator |(WhyNot a, WhyNot b) => (bool)a | (bool)b ? (WhyNot)true : a + "&" + b;

}
