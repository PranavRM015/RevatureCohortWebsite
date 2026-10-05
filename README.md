# Revature Cohort Website

## Overview

The current MVP is a site where our cohort can log in w/ Discord and ask questions about class notes and get answers that cite the actual note. Right now our communication is on Discord and our notes live in our trainer's GitHub, so everything is spread out. This puts it in one place and gives me practice w/ the stack we're learning.

Our cohort is splitting into a React track and an Angular track, so the site has to know which track you're in and answer from that track's notes.

This is not a Discord replacement. Chat stays on Discord.

## Tech Stack and Architecture

The frontend is an Angular SPA and the backend is an ASP.NET Core Web API. No MVC views, since Angular handles all the UI. The Angular build gets served out of the API's wwwroot, so it ships as one app.

| Layer | Tech |
| --- | --- |
| Frontend | Angular, TypeScript, HTML/CSS |
| Backend | C#, ASP.NET Core Web API (controllers) |
| ORM | EF Core, code-first migrations |
| Database | Azure SQL (SQL Server) |
| Auth | Discord OAuth2, cookie auth |
| Hosting | Azure App Service |
| CI/CD | GitHub Actions |
| LLM / embeddings | TBD, decided at the RAG stage |

Why this setup:

- Same origin for frontend and API, so no CORS setup and I can use HttpOnly cookies instead of storing JWTs in the browser.
- In local dev I run `ng serve` w/ a proxy that forwards `/api/*` to the API, so I still get hot reload.
- `MapFallbackToFile("index.html")` sends Angular routes to the SPA instead of 404ing.
- Azure SQL has a native vector type, so I can probably keep the embeddings in the same DB instead of adding a separate vector store. 
- Secrets (Discord client secret, connection string, LLM key) go in user-secrets locally and App Service settings in prod. Nothing gets committed.
- Please note that cloud hosting, Azure SQL LLM / embeddings will be dealt with after the conclusion of this MVP.

## Roles and Tracks

There are three roles and two tracks. Login is Discord only, and you have to be in our cohort's Discord server to get in.

| Role | Who | Can do |
| --- | --- | --- |
| Trainee | Everyone in the cohort | Ask questions, view profiles |
| Trainer | Our trainer | Everything a trainee can do |
| Admin | Me | Everything + manage users, roles, and re-run note ingestion |

Track is React or Angular. I'm planning to read it off your Discord role when you log in, so nobody has to set it manually. Your track decides which notes the Q&A answers from. Users can also set their location to the location they will be deployed.

Why no passwords: Discord login already restricts the site to cohort members. Adding passwords would mean hashing, password resets (which needs email), lockout, and linking accounts if someone uses both. Not worth it for the MVP.

## User Stories

**1. Study from the Claude document**

As a trainee, I want to access all the information from the cohort's syllabus so that I can navigate the topics and test myself on what was taught through multiple choice questions.

Acceptance Criteria:

- The site includes information about each topic covered to this date.
- The link is only visible to logged-in cohort members.
- This is mandatory for the MVP. Practice QCs are out of scope for the time being.

**2. View cohort profiles and portfolios**

As a cohort member, I want to view everyone's profile and portfolio so that I know who's in the cohort, can reach out to them, and can see their work.

Acceptance Criteria:

- Each profile links to that person's portfolio (the profile link).
- Members can edit their own profile color and portfolio link.
- Each profile color is unique and visibly different from everyone else's.
- Profiles are only visible to logged-in cohort members.

## MVP Feature List

In the MVP:

1. Discord login, gated to our cohort's server
2. Profiles w/ unique colors and a place for everyone to reach each other's portfolios
3. A link to the Claude document

Not in the MVP:

- Topic navigation and AI-generated memorization games built into the site. People asked for these and they'd be great, but for now the Claude document covers it. Post-MVP.
- Quiz/flashcard feature. I want something like Quizlet built in, but Quizlet has no usable public API, so this would be our own quiz feature that imports questions from JSON. Post-MVP.
- QC feedback and performance tracking. Dropped for now since that's Revature's evaluation data.
- Chat moderation. Discord AutoMod already covers this.
- Chat in general. That stays on Discord.
- Azure hosting; this will be done via Render for the MVP.

## Data Model

No password column since login is Discord only.

| Table | Columns | Notes |
| --- | --- | --- |
| User | DiscordId (PK), Username, AvatarUrl, ProfileColor (unique), Role, TrackName, CreatedAt, ProfileLink | DiscordId is a snowflake, so it's stored as a string and used as the PK (no separate Id); Track name can only be React or Angular |

Profile color: Discord gives us `accent_color` from the banner, but it's null for most people. If it's null I'll generate one from the user's Discord ID. Uniqueness is checked by perceptual distance (not exact hex match), since two colors 1 hex value apart look identical. Lightness is clamped so text stays readable.

## What Needs to Be Incorporated

- Finalized details: locked doc, trainer's OK, Discord app registered, repo set up
- Web API project
- EF Core entities and the first migration
- Angular app w/ routing, calling the real API through the dev proxy
- Discord login w/ the server check
- Profiles w/ profile colors
- Deployment: Angular build into wwwroot, App Service + Azure SQL, Discord redirect URI for prod
- Portfolio access: profile page showing each member's profile link

## Team Workflow

We're about 20 people, which is a lot for one weekend, so the rules have to be simple.

- `main` is protected. No direct pushes, every change goes through a PR.
- Every PR needs 1 approval and a passing CI build.
- One GitHub issue per task, and you assign yourself before starting so two people don't build the same thing.
- Small teams own one area each: backend/DB, frontend, auth + profiles, deploy, RAG.
- Local setup is SQL Server in Docker + `ng serve`, documented in the README so anyone can get running.

