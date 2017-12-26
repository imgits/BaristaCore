﻿namespace BaristaLabs.BaristaCore.Utils
{
    using BaristaLabs.BaristaCore.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Utility method to help with obtaining member info of a type.
    /// </summary>
    internal sealed class ObjectReflector
    {
        private readonly Type m_type;

        public ObjectReflector(Type t)
        {
            m_type = t ?? throw new ArgumentNullException(nameof(t));
        }

        public IEnumerable<ConstructorInfo> GetConstructors()
        {
            return m_type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(c => c.GetCustomAttribute<BaristaIgnoreAttribute>() == null);
        }

        /// <summary>
        /// Returns the best constructor given the specified arguments.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public ConstructorInfo GetConstructorBestMatch(object[] args)
        {
            var argTypes = args.Select(arg => arg.GetType()).ToArray();
            var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            var matchingConstructor = m_type.GetConstructor(bindingFlags, null, argTypes, null);
            if (matchingConstructor != null && matchingConstructor.GetCustomAttribute<BaristaIgnoreAttribute>() == null)
                return matchingConstructor;

            var constructors = GetConstructors();
            ConstructorInfo bestMatch = null;
            var bestMatchMatchingArgs = 0;
            foreach (var constructor in constructors)
            {
                var constructorParams = constructor.GetParameters();

                //Default constructor, use it if another, better constructor hasn't already been set.
                if (bestMatch == null && constructorParams.Length == 0)
                {
                    bestMatch = constructor;
                    bestMatchMatchingArgs = 0;
                    continue;
                }

                var currentMatchingArgs = GetMatchingParameterCount(constructorParams, argTypes, out bool isExact);
                if (currentMatchingArgs > bestMatchMatchingArgs || (currentMatchingArgs == bestMatchMatchingArgs && isExact))
                {
                    bestMatch = constructor;
                    bestMatchMatchingArgs = currentMatchingArgs;
                    continue;
                }
            }

            return bestMatch;
        }

        public IEnumerable<PropertyInfo> GetProperties(bool instance)
        {
            PropertyInfo[] properties;
            if (instance == false)
                properties = m_type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            else
                properties = m_type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            return properties.Where(p => p.GetCustomAttribute<BaristaIgnoreAttribute>() == null);
        }

        public IDictionary<string, IList<MethodInfo>> GetUniqueMethodsByName(bool instance)
        {
            MethodInfo[] methods;
            if (instance == false)
                methods = m_type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            else
                methods = m_type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            methods = methods.Where(p => p.GetCustomAttribute<BaristaIgnoreAttribute>() == null).ToArray();

            var methodTable = new Dictionary<string, IList<MethodInfo>>();
            foreach(var methodInfo in methods)
            {
                var methodName = BaristaPropertyAttribute.GetMemberName(methodInfo);
                if (methodTable.ContainsKey(methodName))
                {
                    methodTable[methodName].Add(methodInfo);
                }
                else
                {
                    methodTable.Add(methodName, new List<MethodInfo>() { methodInfo });
                }
            }

            return methodTable;
        }

        public MethodInfo GetMethodBestMatch(IEnumerable<MethodInfo> methods, object[] args)
        {
            var argTypes = args.Select(arg => arg.GetType()).ToArray();
            MethodInfo bestMatch = null;
            var bestMatchMatchingArgs = 0;
            foreach (var method in methods)
            {
                var methodParams = method.GetParameters();

                var currentMatchingArgs = GetMatchingParameterCount(methodParams, argTypes, out bool isExact);
                if (currentMatchingArgs > bestMatchMatchingArgs || (currentMatchingArgs == bestMatchMatchingArgs && isExact))
                {
                    bestMatch = method;
                    bestMatchMatchingArgs = currentMatchingArgs;
                    continue;
                }
            }

            return bestMatch;
        }

        public IDictionary<string, EventInfo> GetEventTable(bool instance)
        {
            EventInfo[] events;
            if (instance == false)
                events = m_type.GetEvents(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            else
                events = m_type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            events = events.Where(p => p.GetCustomAttribute<BaristaIgnoreAttribute>() == null).ToArray();

            var eventTable = new Dictionary<string, EventInfo>();
            foreach (var eventInfo in events)
            {
                var eventName = BaristaPropertyAttribute.GetMemberName(eventInfo);
                eventTable.Add(eventName, eventInfo);
            }

            return eventTable;
        }

        public Type GetBaseType()
        {
            return m_type.BaseType;
        }

        private int GetMatchingParameterCount(ParameterInfo[] parameters, Type[] argTypes, out bool isExact)
        {
            var count = 0;
            isExact = true;
            for (int i = 0; i < parameters.Length; i++)
            {
                //If there are more parameters than arguments, we're no longer exact (and stop processing)
                if (i >= argTypes.Length)
                {
                    isExact = false;
                    break;
                }

                //If there is an exact type match, we're still exact.
                if (parameters[i].ParameterType == argTypes[i])
                {
                    count++;
                }
                //We're checking numeric types, no longer exact.
                else if (parameters[i].ParameterType.IsNumeric() && argTypes[i].IsNumeric())
                {
                    isExact = false;
                    count++;
                }
            }

            //If we had no matches, but we did have parameters, we're not exact.
            if (count == 0 && parameters.Length > 0)
                isExact = false;

            return count;
        }
    }
}