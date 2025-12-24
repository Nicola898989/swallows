# 🕊️ Swallows - Advanced SEO Crawler & Analyzer

<div align="center">

![Swallows Logo](Assets/swallows.png)

**A powerful, cross-platform desktop application for comprehensive website SEO analysis, web crawling, and AI-powered insights.**

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-11.x-8B44AC?logo=avalonia)](https://avaloniaui.net/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

[Features](#-features) • [Installation](#-installation) • [User Guide](#-user-guide) • [AI Integration](#-ai-integration) • [Testing](#-testing)

</div>

---

## ✨ Features

### � Core Crawling Engine
- **High-Performance Crawling**: multithreaded architecture for fast scanning.
- **Deep Scanning**: Configurable depth and page limits.
- **JavaScript Rendering**: Integrated headless browser support for crawling heavy SPA (React, Angular, Vue) sites.
- **Robots.txt Compliance**: Automatically respects exclusion rules.
- **Real-time Control**: Pause, Resume, and Stop functionality.

### 📊 SEO Analysis Dashboard
- **Real-time Statistics**: Monitor pages scanned, queue size, and performance metrics live.
- **Interactive Charts**:
  - **Crawl Depth Distribution**: Visualize site architecture depth.
  - **Response Time Trend**: Track performance bottlenecks.
- **Detailed Data Grid**: Sortable, filterable view of all scanned pages with 40+ data points (Meta tags, Headers, Word count, Status codes, etc.).

### 🧠 AI-Powered Analysis
- **Local LLM Integration**: Connects with [Ollama](https://ollama.com/) for privacy-focused, offline AI analysis.
- **Smart Audits**: Ask AI to analyze specific pages for content quality, sentiment, or keyword optimization.
- **Custom Prompts**: Define your own analysis criteria.

### 📈 Advanced Modules
- **History Management**: Automatically saves all scan sessions. Review, delete, or resume past scans.
- **Scan Comparison**: Side-by-side diff of two scans to track SEO progress or regressions over time.
- **Trend Analysis**: Visualize historical performance (Errors, Scores, Load Times) for a specific domain.
- **Structure Visualization**: Interactive tree view of your website's hierarchy.
- **SEO Audit Report**: Dedicated view with 6+ charts breaking down site health (Status codes, SEO Scores, Issues).

### 💾 Export & Integration
- **Multiple Formats**: Export to CSV, Excel (.xlsx), or generate standard `sitemap.xml`.
- **Graph Export**: Visual representation of internal linking structure.

---

## 🚀 Installation

### Prerequisites
- **OS**: Verified on macOS Sequoia 15.2, Windows 11, and Linux.
- **.NET 9.0 SDK**: [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)

### Getting Started

1. **Clone the repository:**
   ```bash
   git clone https://github.com/yourusername/swallows.git
   cd swallows
   ```

2. **Build the project:**
   ```bash
   dotnet build
   ```

3. **Run the application:**
   ```bash
   dotnet run --project src/Swallows.Desktop
   ```

---

## 📖 User Guide

### 1. Dashboard & Scanning
The **Main Dashboard** is your command center.
- **Start a Scan**: Enter a URL in the top bar (e.g., `https://example.com`).
- **Configuration**:
  - **User Agent**: Select specific bots (Googlebot, Bingbot) or standard browsers.
  - **Controls**: Use `▶️` to start, `⏸️` to pause/resume, and `⏹️` to stop.
- **Live Monitoring**: 
  - **Top Cards**: Show Pages Scanned, Remaining Queue, Predicted Total, and Progress/Status.
  - **Charts**: Watch "Crawl Depth" and "Response Time" graphs update in real-time.
- **Results Grid**: click any column header to sort. Toggle columns visibility via the "�️ Columns" dropdown.

### 2. Settings Configuration (`Ctrl+S`)
Customize the crawler behavior in the **Settings** window:
- **General**:
  - **Max Pages**: Limit the total number of pages to scan.
  - **Max Depth**: Limit how many clicks deep to crawl.
  - **Concurrent Requests**: Set the number of parallel threads (higher = faster, but heavier on server).
  - **Timeouts**: Set connection timeout duration.
- **Advanced**:
  - **Enable JavaScript Rendering**: Check this to crawl sites that require JS to load content.
  - **Extraction Rules**: Define custom XPath/CSS selectors to extract specific data from pages.

### 3. SEO Audit (`Ctrl+A`)
Access a high-level health report of your scan.
- Click **"📊 SEO Audit"** in the tool bar after selecting a scan.
- **Charts included**:
  - SEO Score Distribution
  - HTTP Status Codes (200, 404, 500 etc.)
  - Top Issues (Missing Alt, Duplicate Titles, etc.)
  - Content Categories

### 4. History (`Ctrl+H`)
View and manage your past scans.
- Click **"📜 History"** to see a list of all database-saved sessions.
- **Select** a session to load it back into the dashboard.
- **Delete** old sessions to free up space.

### 5. Comparisons
Track progress over time.
- Click **"⚖️ Compare"**.
- Select a **Baseline Scan** (older) and a **Target Scan** (newer).
- View a diff report showing:
  - New pages added.
  - Pages removed.
  - Changes in SEO Scores or Status Codes.

### 6. Trend Analysis
Visualize the trajectory of a website's health.
- Click **"📈 Trends"**.
- Enter a base URL.
- The system plots historical data points from all matching scans in your history, showing trends for **SEO Score**, **Errors**, and **Load Time**.

### 7. Structure View
- Click **"🕸️ Structure"**.
- Explore an interactive tree diagram of the website's folder structure and hierarchy.

### 8. Export Data (`Ctrl+E`)
Get your data out of Swallows.
- **📄 CSV**: Export raw tabular data.
- **📊 Excel**: Export a formatted report with multiple sheets.
- **🗺️ Sitemap**: Generate a standard `sitemap.xml` for submission to search engines.
- **🕸️ Graph**: Export the link graph structure.

---

## 🧠 AI Integration (Ollama)

Swallows integrates with **Ollama** to bring local AI capabilities to your SEO workflow.

### Setup
1. Download and install [Ollama](https://ollama.com/).
2. Pull a model (e.g., `ollama pull llama3`).
3. In Swallows, go to **Settings** -> **AI** and entering your model name.

### Using AI Analysis
1. Select a page in the Results Grid.
2. Click **"🧠 AI Auditor"**.
3. Choose a prompt (e.g., "Analyze for keyword density", "Check tone of voice") or write a custom one.
4. The AI will analyze the page content and provide specific recommendations.

---

## 🧪 Testing

Swallows includes a comprehensive UI automation test suite.

### Running Tests
To run the full suite of UI tests:

```bash
dotnet test src/Swallows.Tests/Swallows.Tests.csproj
```

The tests cover:
- End-to-End scan workflows.
- Database persistence.
- ViewModel logic and all UI windows.

---

## 📄 License

This project is licensed under the MIT License.
