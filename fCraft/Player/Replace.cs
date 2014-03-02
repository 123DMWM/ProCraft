﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class Extensions
{
    public static string ReplaceString(this string str, string oldValue, string newValue, StringComparison comparison)
    {
        StringBuilder sb = new StringBuilder();

        int previousIndex = 0;
        int index = str.IndexOf(oldValue, comparison);
        while (index != -1)
        {
            sb.Append(str.Substring(previousIndex, index - previousIndex));
            sb.Append(newValue);
            index += oldValue.Length;

            previousIndex = index;
            index = str.IndexOf(oldValue, index, comparison);
        }
        sb.Append(str.Substring(previousIndex));

        return sb.ToString();
    }
}