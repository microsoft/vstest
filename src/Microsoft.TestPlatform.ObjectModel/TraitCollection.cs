// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel;

#if NETFRAMEWORK
using System;
#endif
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Class that holds collection of traits
/// </summary>
#if NETFRAMEWORK
[Serializable]
#endif
public class TraitCollection : IEnumerable<Trait>
{
    internal const string TraitPropertyId = "TestObject.Traits";
    private static readonly TestProperty TraitsProperty = TestProperty.Register(
        TraitPropertyId,
#if !NET451
        // TODO: Fix this with proper resourcing for UWP and Win 8.1 Apps
        // Trying to access resources will throw "MissingManifestResourceException" percolated as "TypeInitialization" exception
        "Traits",
#else
        Resources.Resources.TestCasePropertyTraitsLabel,
#endif
        typeof(KeyValuePair<string, string>[]),
#pragma warning disable 618
        TestPropertyAttributes.Hidden | TestPropertyAttributes.Trait,
#pragma warning restore 618
        typeof(TestObject));

#if NETFRAMEWORK
    [NonSerialized]
#endif
    private readonly TestObject _testObject;

    internal TraitCollection(TestObject testObject)
    {
        ValidateArg.NotNull(testObject, nameof(testObject));

        _testObject = testObject;
    }

    public void Add(Trait trait)
    {
        ValidateArg.NotNull(trait, nameof(trait));

        AddRange(new[] { trait });
    }

    public void Add(string name, string value)
    {
        ValidateArg.NotNull(name, nameof(name));

        Add(new Trait(name, value));
    }

    public void AddRange(IEnumerable<Trait> traits)
    {
        ValidateArg.NotNull(traits, nameof(traits));

        var existingTraits = GetTraits();
        Add(existingTraits, traits);
    }

    public IEnumerator<Trait> GetEnumerator()
    {
        var enumerable = GetTraits();
        return enumerable.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private IEnumerable<Trait> GetTraits()
    {
        if (!_testObject.Properties.Contains(TraitsProperty))
        {
            yield break;
        }

        var traits = _testObject.GetPropertyValue(TraitsProperty, Enumerable.Empty<KeyValuePair<string, string>>().ToArray());

        foreach (var trait in traits)
        {
            yield return new Trait(trait);
        }
    }

    private void Add(IEnumerable<Trait> traits, IEnumerable<Trait> newTraits)
    {
        var newValue = traits.Union(newTraits);
        var newPairs = newValue.Select(t => new KeyValuePair<string, string>(t.Name, t.Value)).ToArray();
        _testObject.SetPropertyValue(TraitsProperty, newPairs);
    }
}
