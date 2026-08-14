/////////////////////////////////////////////////////////////////////////////////
//
//	FoldoutAttribute.cs
//
//	Description:	part of the foldout system.
//					
/////////////////////////////////////////////////////////////////////////////////

using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class FoldoutAttribute : PropertyAttribute
{
    public string GroupName;

    public FoldoutAttribute(string groupName)
    {
        GroupName = groupName;
    }

    public class EndFoldoutAttribute : PropertyAttribute { }
}
