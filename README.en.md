# ExcelFileMappingEngine

.NET WPF application for automated Excel file transformation and mapping.

![Workflow](Docs/workflow.png)
---

## About the Project

This project was created from a real workplace need — repetitive manual adjustment of Excel files with the same or similar structure.

Since this process had to be repeated regularly, I decided to use my newly acquired programming skills to create a tool that could automate this work.

The first prototype was created as a Windows Forms application using .NET Core. It solved the immediate problem, but it was highly specific and tightly coupled to my personal Excel files.

Because of that, I decided to start over and build a more flexible solution — a tool that could be used to transform and prepare different Excel files according to user-defined rules.

---

## Project Idea

The main concept:

1. The user opens an Excel file in the application.
2. The user configures the file according to their needs:

   * selects the correct header row;
   * removes unnecessary columns;
   * adds new columns;
   * renames columns;
   * performs other required transformations.
3. The user saves the created mapping configuration.
4. The next time the same type of file is received, the user only needs to:

   * open the file;
   * select the required mapping;
   * generate the already prepared result.

The goal is to replace repetitive manual Excel preparation with an automated and reusable process.

---

## Current Development Status

The project is currently under active development.

At this stage, the application is able to:

* open Excel files;
* export processed results;
* select the Excel header row;
* modify table structure;
* delete columns;
* add new columns;
* rename columns;
* save performed actions into JSON files;
* apply previously saved mappings.

Before the current architecture refactoring, the saved mappings were already successfully tested in real usage scenarios.

---

## Architecture and Refactoring

After creating the first working prototype, the decision was made to gradually rebuild the project with a cleaner and more maintainable architecture.

The current focus is improving the structure to:

* separate UI logic from business logic;
* create clear responsibilities between classes;
* make the application easier to maintain and extend;
* prepare the project for future functionality.

Main components:

* **WPF UI** — handles user interaction and visual presentation;
* **AppManager** — coordinates application workflows and business operations;
* **ExcelService** — handles Excel data processing;
* **FileState** — stores the state of the currently opened file.

---

## Data Processing Approach

To avoid unnecessary reloading of Excel files, the data processing flow is separated into two layers:

### RawData

The original data loaded from the Excel file.

### CurrentData

The currently processed version shown to the user.

This approach allows:

* changing the header row without reopening the file;
* rebuilding the table structure from the original data;
* keeping the original source data unchanged.

---

## Future Plans

Planned features include:

* saving and applying formulas;
* calculation-based transformations;
* using additional files as data sources for mappings;
* filling columns based on values from other columns;
* more advanced transformation scenarios.

Example:

If an Excel file contains customer data, the application could automatically assign the correct manager to each customer using an additional data source.

---

## Validation and Safety

One of the future goals is creating a validation system to prevent incorrect mapping usage.

Planned features:

* checking whether a mapping matches the selected file structure;
* showing only relevant mapping options to the user;
* preventing incorrect configurations from being applied;
* providing clear warnings and validation messages.

The goal is to make the application safer and easier to use by guiding the user instead of letting them try random configurations.

---

## Goal

The goal is to create a flexible Excel data transformation tool that can adapt to different file structures and significantly reduce repetitive manual work.

This project is being developed not only as a practical workplace tool but also as a learning project for improving software development skills and applying better programming practices.

