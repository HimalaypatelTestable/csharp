using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Library.Model;

public class Category
{
    private static readonly Regex HexColorRegex = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    private readonly List<Category> _subCategories = new();
    private readonly List<string> _bookIds = new();
    private string _name;
    private string? _colorHex;
    private Category? _parent;

    public Category(string id, string name)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Category id cannot be null or blank", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be null or blank", nameof(name));

        Id = id;
        _name = name;
        Active = true;
    }

    public string Id { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Name cannot be null or blank", nameof(value));
            _name = value;
        }
    }

    public string? Description { get; set; }

    public Category? Parent
    {
        get => _parent;
        set
        {
            if (value == this)
                throw new ArgumentException("Category cannot be its own parent", nameof(value));
            if (value is not null && value.IsDescendantOf(this))
                throw new ArgumentException("Cannot create cyclic category hierarchy", nameof(value));
            _parent?._subCategories.Remove(this);
            _parent = value;
            if (value is not null && !value._subCategories.Contains(this))
                value._subCategories.Add(this);
        }
    }

    public IReadOnlyList<Category> SubCategories => _subCategories.AsReadOnly();

    public IReadOnlyList<string> BookIds => _bookIds.AsReadOnly();

    public int DisplayOrder { get; set; }

    public bool Active { get; set; }

    public string? IconCode { get; set; }

    public string? ColorHex
    {
        get => _colorHex;
        set
        {
            if (value is not null && !HexColorRegex.IsMatch(value))
                throw new ArgumentException($"Invalid color hex: {value}", nameof(value));
            _colorHex = value;
        }
    }

    public int PopularityScore { get; private set; }

    public bool IsRoot => _parent is null;

    public bool IsLeaf => _subCategories.Count == 0;

    public int DirectBookCount => _bookIds.Count;

    public int Depth
    {
        get
        {
            var depth = 0;
            var current = _parent;
            while (current is not null)
            {
                depth += 1;
                current = current._parent;
            }
            return depth;
        }
    }

    public string FullPath => _parent is null ? _name : $"{_parent.FullPath} > {_name}";

    public bool IsDescendantOf(Category? other)
    {
        if (other is null) return false;
        var current = _parent;
        while (current is not null)
        {
            if (current.Equals(other))
                return true;
            current = current._parent;
        }
        return false;
    }

    public void AddSubCategory(Category subCategory)
    {
        ArgumentNullException.ThrowIfNull(subCategory);
        subCategory.Parent = this;
    }

    public bool RemoveSubCategory(Category subCategory)
    {
        if (_subCategories.Remove(subCategory))
        {
            subCategory._parent = null;
            return true;
        }
        return false;
    }

    public void AddBookId(string bookId)
    {
        if (string.IsNullOrWhiteSpace(bookId))
            throw new ArgumentException("Book id cannot be null or blank", nameof(bookId));
        if (!_bookIds.Contains(bookId))
        {
            _bookIds.Add(bookId);
            PopularityScore += 1;
        }
    }

    public bool RemoveBookId(string bookId) => _bookIds.Remove(bookId);

    public int GetTotalBookCount()
    {
        var total = _bookIds.Count;
        foreach (var sub in _subCategories)
            total += sub.GetTotalBookCount();
        return total;
    }

    public void IncrementPopularity() => PopularityScore += 1;

    public override bool Equals(object? obj) => obj is Category c && c.Id == Id;

    public override int GetHashCode() => Id.GetHashCode();

    public override string ToString()
        => $"Category{{Id='{Id}', Path='{FullPath}', DirectBooks={_bookIds.Count}, SubCategories={_subCategories.Count}}}";
}
