# Data Science Workbench - User Guide

## Overview

The Data Science Workbench provides an integrated Python editor designed for data analysis. It combines a full-featured code editor with pre-loaded datasets, making it easy to explore, analyze, and visualize data without any setup.

This guide covers all three tabs of the workbench: the **Python Editor**, the **Data Reference** browser, and the **Package Manager**.

---

## Getting Started

When you open the workbench, you'll see a Python script editor with some sample code already loaded. Your datasets are pre-loaded as variables and ready to use immediately.

To run your first script:
1. Click in the editor area
2. Press **F5** (or use the **Run** menu and select **Execute Script**)
3. View the results in the **Output** panel below the editor

---

## Tab 1: Python Editor

### Writing Code

The editor works like a standard code editor with features designed for Python:

- **Syntax highlighting** colors your code automatically (keywords in blue, strings in red, comments in green, etc.)
- **Autocomplete** pops up as you type, suggesting variable names, column names, Python keywords, and more. Press **Tab** or **Enter** to accept a suggestion, or **Escape** to dismiss it.
- **Error detection** underlines problems in your code:
  - Red underlines indicate syntax errors
  - Blue underlines indicate potentially undefined variables
  - Hover your mouse over an underline to see the error details

### Using Your Data

Datasets are pre-loaded as variables. You can access them directly:

```python
# See how many records are in a dataset
print(len(customers))

# Access a specific column
print(customers.CreditLimit.mean())

# Get the full DataFrame for advanced operations
print(customers.df.describe())
print(customers.df.groupby('Region')['CreditLimit'].mean())
```

### Creating Plots

The workbench supports matplotlib plotting. When your script calls `plt.show()`, a viewer window opens with your plot:

```python
import matplotlib.pyplot as plt

plt.figure(figsize=(10, 6))
plt.hist(customers.CreditLimit, bins=20)
plt.title('Credit Limit Distribution')
plt.xlabel('Credit Limit')
plt.ylabel('Count')
plt.show()
```

If your script generates multiple plots, use the **Previous** and **Next** buttons in the viewer to browse them. Click **Save** to export any plot as a PNG image.

### Code Snippets

Not sure where to start? Use the **Insert Snippet** menu in the editor toolbar. It offers ready-made templates for common tasks:

- **List Datasets** - See what data is available
- **Basic Statistics** - Summary statistics for your data
- **Plot Histogram** - Create a histogram
- **Scatter Plot** - Create a scatter plot
- **Group By Analysis** - Aggregate data by category
- **Correlation Matrix** - Find relationships between columns
- **Time Series Plot** - Plot data over time

### Bookmarks

Mark important lines in your code for quick navigation:

- **Ctrl+B** to toggle a bookmark on the current line (a blue circle appears in the gutter)
- **F2** to jump to the next bookmark
- **Shift+F2** to jump to the previous bookmark

### Find & Replace

Press **Ctrl+H** to open the Find & Replace panel. Type your search term, optionally enter a replacement, and use the buttons to find or replace matches.

---

## Tab 2: Data Reference

The Data Reference tab is your guide to all available data. It displays a tree view showing:

- **Datasets** and their columns (with data types)
- **Registered Python classes** (if the host application has added any)
- **Context variables** (values passed in from the host application)

### How to Use

1. Click on any item in the tree to see details in the panel below
2. The detail panel shows:
   - A description of the selected item
   - The Python data type
   - Example code you can copy into the editor
3. Use the **search box** at the top to filter items by name

This tab is read-only and is there for reference only. It helps you discover what data and tools are available without needing to memorize variable names.

---

## Tab 3: Package Manager

The Package Manager lets you install and remove Python packages (libraries) to extend what you can do in your scripts.

### Installing a Package

1. Type the package name in the **Package name** field (e.g., `seaborn`, `scikit-learn`, `scipy`)
2. Click **Install**
3. Wait for the installation to complete (progress appears in the output)
4. The package is now available to import in your scripts

Alternatively, use the **Quick Install** dropdown to select from commonly used data science packages and click the install button next to it.

### Uninstalling a Package

1. Select the package you want to remove from the installed packages list
2. Click **Uninstall**
3. The package will be removed from the environment

### Searching Packages

Use the **search box** above the package list to filter by name. Type any part of a package name to narrow the list. Clear the search box to see all packages again.

### Refreshing the List

Click **Refresh** to reload the list of installed packages. This is useful after installing or uninstalling packages.

---

## Keyboard Shortcuts Reference

| Shortcut | Action |
|---|---|
| **F5** | Run script |
| **Ctrl+S** | Save script to file |
| **Ctrl+O** | Open script from file |
| **Ctrl+Z** | Undo |
| **Ctrl+Y** | Redo |
| **Ctrl+X** | Cut |
| **Ctrl+C** | Copy |
| **Ctrl+V** | Paste |
| **Ctrl+A** | Select all |
| **Ctrl+H** | Find & Replace |
| **Ctrl+D** | Duplicate current line |
| **Alt+Up** | Move line up |
| **Alt+Down** | Move line down |
| **Tab** | Indent selected lines |
| **Shift+Tab** | Unindent selected lines |
| **Ctrl+B** | Toggle bookmark |
| **F2** | Next bookmark |
| **Shift+F2** | Previous bookmark |
| **Ctrl+Mouse Wheel** | Zoom in/out |
| **Escape** | Close autocomplete or Find panel |

---

## Tips and Best Practices

- **Start with snippets.** If you're new to Python or data analysis, use Insert Snippet to get working code you can modify.
- **Use the Data Reference tab.** Before writing code, browse the Data Reference tab to see exactly what columns and data types are available.
- **Check for errors before running.** The editor underlines problems in real-time. Hover over red or blue underlines to understand the issue.
- **Use autocomplete.** After typing a dataset name and a dot (e.g., `customers.`), the autocomplete will show available columns and methods.
- **Save your work.** Use **Ctrl+S** to save your script to a file. Use **Ctrl+O** to open a previously saved script.
- **Explore with `print()`.** Use `print()` statements to inspect your data at each step.
- **Use `.df` for advanced operations.** Each dataset has a `.df` property that gives you the full pandas DataFrame for grouping, filtering, merging, and more.

---

## Troubleshooting

| Problem | Solution |
|---|---|
| "Python not available" message | Python could not be found on the system. Contact your administrator. |
| Script runs but no output appears | Make sure you use `print()` to display results. |
| Package installation fails | Check your internet connection. Some packages may have system-level dependencies. |
| Plot doesn't appear | Ensure your script calls `plt.show()` after creating the plot. |
| Autocomplete not showing | Make sure the cursor is in the editor. Try typing a few characters first. |
| Red underlines in code | These indicate syntax errors. Hover over them to see details and fix the issue. |
