# 👨‍🍳 RecipeSharer: Secure MVC & JWT Platform

This is a site I made using the MVC pattern to hold some of my enjoyed recipes with user authentication using JSON Web Tokens.

<img width="600" height="453" alt="recipe" src="https://github.com/user-attachments/assets/85c03bab-ce42-4b00-adf2-07bfa7ead1b4" text-align="center"/>

Self hosted on my subdomain https://recipes.jamieandrews.co.uk

A full-stack Recipe Sharing platform built with **ASP.NET Core MVC**, implementing a robust security architecture that bridges traditional Cookie-based authentication with modern **JWT (JSON Web Tokens)**.

---

## 🔐 Security & Identity Architecture
This project focuses heavily on the "AAA" of security (Authentication, Authorization, and Accounting):

*   **ASP.NET Core Identity:** Fully implemented user management system including password hashing, lockout policies, and unique email enforcement.
*   **Hybrid Authentication:** 
    *   **Cookie Auth:** Configured for traditional MVC web navigation.
    *   **JWT Bearer Tokens:** Integrated using `Microsoft.AspNetCore.Authentication.JwtBearer`, allowing the platform to support potential mobile or decoupled frontend clients.
*   **Strict Security Policies:** Custom Identity options enforced for complexity (uppercase, alphanumeric) and security (lockout after 10 failed attempts).
*   **Markdig Integration:** Utilizes the Markdig library with a custom MarkdownPipeline to convert Markdown strings into sanitized HTML. This allows for advanced formatting (tables, task lists, etc.) while maintaining control over the output.

## 🛠️ Technical Stack
*   **Framework:** ASP.NET Core 9.0 MVC
*   **Database:** SQLite (Entity Framework Core) — chosen for portability and lightweight local development.
*   **Testing:** **xUnit** for unit testing business logic and controller behavior.
*   **Frontend:** Razor Pages with JavaScript for dynamic client-side interactions.
*   **DI (Dependency Injection):** 
    *   `IFileService` (Scoped): Handles recipe image uploads.
    *   `TokenGenerator` (Singleton): Centralized logic for secure JWT creation.

---

## 🧪 Testing & Reliability
Quality assurance is a core part of this project. Using **xUnit**, I implemented tests to verify:
*   User registration and login logic.
*   Recipe CRUD operations.
*   Token generation validity.

## 🚀 Key Features
*   **Image Handling:** Integrated a custom `FileService` to manage recipe photo uploads.
*   **Access Control:** Custom `AccessDenied` and `Login` paths for a seamless User Experience.
*   **Database Migrations:** Entity Framework Core used to manage the SQLite schema.
*   **Static Assets:** Utilizes the new `MapStaticAssets` for optimized delivery of CSS/JS.
