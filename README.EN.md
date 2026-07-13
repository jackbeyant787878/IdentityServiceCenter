IdentityService – Distributed Permission & Identity Center | Technical Architecture Documentation
IdentityService is a lightweight, highly extensible, distributed unified authentication & authorization center built on .NET 10. Strictly designed with DDD (Domain-Driven Design), CQRS read/write separation, and SOLID principles, it implements standard OAuth2.0 / OIDC 1.0 protocols based on OpenIddict 6.0+. It supports SaaS account authentication, cross-service authorization in microservice clusters, and a dual-token mechanism, providing standardized, secure, and enterprise-grade identity & access control for distributed architectures.
Core Positioning: Unified identity authentication, distributed permission governance, microservice security gateway, SaaS multi-tenant identity infrastructure
Applicable Scenarios: Microservice clusters, SaaS multi-tenant platforms, front-end/back-end separated systems, enterprise-level distributed applications

---
📋 Table of Contents
- Architecture Design (DDD / CQRS / SOLID)
- Core Authentication & Authorization Mechanism
- Supported Authentication & Authorization Models
- Token System (JWT / AccessToken / RefreshToken)
- RSA Key Containerized Management & Smooth Rotation (Core Highlights)
- Technology Stack Selection
- Core API Principles & Source Implementation
- Quick Start
- Core Project Advantages
- Open Source License

---
🏗️ Architecture Design
This project abandons the bloated and tightly coupled traditional layered architecture. It adopts a three-in-one architecture system of DDD domain-driven design + CQRS read/write separation + SOLID principles, achieving high cohesion, low coupling, strong scalability, and maintainability for enterprise-level permission services.
1. Domain-Driven Design (DDD)
Centered on core domain models including permissions, identities, tenants, roles, and users, the architecture splits bounded contexts clearly and eliminates the confusion and ambiguous responsibilities of traditional permission systems.
- Domain Layer: Contains core business entities (users, roles, permissions, tenants), domain rules, and domain events. No external framework dependencies, pure business logic to ensure stability.
- Application Layer: Coordinates domain logic and request orchestration without holding core business rules. Uses MediatR for event decoupling and encapsulates all Command/Query use cases.
- Infrastructure Layer: Implements database persistence, caching, third-party integration, token generation/validation, and OpenIddict adaptation.
- Presentation / HttpApi Layer: .NET 10 Web API controllers responsible only for protocol reception, parameter forwarding, and response output — zero business logic.
Based on DDD domain events, asynchronous notifications are triggered for user creation, role modification, permission updates, and tenant configuration changes to support cross-service data synchronization in distributed systems.
2. CQRS Read/Write Separation Architecture
Strictly separates read operations (Query) and write operations (Command) to resolve read/write contention, performance bottlenecks, and accumulating business complexity, fully aligned with the current source code structure.
- Command (Write): Token exchange, token revocation, client registration, logout, and other state-changing operations, ensuring data consistency and transaction integrity.
- Query (Read): Identity query, permission parsing, and other read scenarios with cache acceleration support.
MediatR mediator pattern drives CQRS scheduling. Controllers never directly invoke services or contain business logic — all requests are processed in the Application layer, achieving complete decoupling of requests and business implementation.
3. SOLID Principle Implementation
The project strictly follows the five SOLID principles and leverages C# 14 features to optimize code structure:
- Single Responsibility: ConnectController handles only OIDC protocol endpoints; ClientApplicationController manages only client registration — fully isolated responsibilities.
- Open/Closed: New login modes and authorization types can be extended via new Commands without modifying existing controllers.
- Liskov Substitution: All infrastructure implementations (database, cache, token service) are swappable.
- Interface Segregation: Fine-grained repository and service interfaces with no redundant dependencies.
- Dependency Inversion: Controllers depend on MediatR abstractions instead of concrete business implementations.

---
🔐 Core Authentication & Authorization Mechanism
Built on standard OAuth2.0 / OIDC 1.0 protocols and powered by OpenIddict 6.0+, the system provides a standardized and highly customizable security framework that adapts to both SaaS multi-tenant scenarios and microservice cluster communication.
1. Core Protocol Capabilities
- OAuth2.0: Standard protocol for third-party authorization and cross-resource access, securing service-to-service and client-to-server access.
- OIDC (OpenID Connect): Identity extension based on OAuth2.0 that standardizes user identity information transmission and enables unified single sign-on.
OpenIddict fully manages token issuance, validation, revocation, and state machines. The business layer only handles user legitimacy, tenant validation, and permission injection.
2. Full Authentication & Authorization Pipeline
1. Protocol Reception: ConnectController.Token receives standard OIDC form requests.
2. Business Validation: ExchangeTokenCommand verifies accounts, tenants, risk control rules, and passwords.
3. Identity Construction: Generates ClaimsPrincipal identity context.
4. Protocol Issuance: OpenIddict issues standard AccessToken / RefreshToken.
5. Downstream Verification: Gateway/microservices validate tokens and fetch trusted user info via UserInfo endpoint.
6. Security Destruction: Supports active token revocation and secure logout.

---
⚙️ Supported Authentication & Authorization Models
Two differentiated authorization models are implemented for SaaS end-users and microservice backend clusters, fully covering business access and internal service communication security.
1. SaaS Account & Password Authentication Model (Tenant End Users)
- Endpoint: /api/connect/token (standard password grant)
- Scenarios: Merchant backend consoles, store operators, tenant staff login
- Capabilities: Multi-tenant isolation, account risk freezing, login validation, permission encapsulation
2. Microservice Client Authorization Model (Service-to-Service Communication)
- Workflow: Client application registration + service secret authentication
- Scenarios: Microservice internal calls, background scheduled tasks, internal system integration
- Capabilities: Dynamic client registration, service-level isolation, illegal cluster access prevention

---
🎫 Token System: JWT Dual-Token Mechanism
Based on OpenIddict’s native JWT generation pipeline, the system implements an industry-standard AccessToken + RefreshToken dual-token model, meeting high security requirements for financial and payment scenarios.
1. AccessToken (Short-Lived Business Token)
- Short validity period for business API authentication
- Carries user, tenant, store, role, and permission claims
- Low leakage risk, suitable for front-end storage
2. RefreshToken (Long-Lived Renewal Token)
- Only used for token renewal, not for business authorization
- Persistent storage with active revocation support
- Enables user silent login extension without re-authentication
3. Security Fuse Capability (Unique Implementation)
Supports forced runtime token revocation for risk control emergency scenarios, solving the inherent weakness of traditional non-revocable JWT tokens.

---
🛡️ RSA Key Containerized Management & Smooth Rotation (Core Key Implementation)
Based on OpenIddict’s asymmetric encryption system, this project delivers a production-grade key security management solution. Powered by Tencent Cloud K3s, it implements encrypted key hosting, container persistent mounting, and full-lifecycle automatic key rotation with cross-service smooth compatibility — eliminating plaintext key exposure, forced iteration downtime, and cluster authentication failures.
1. Core Asymmetric Key Separation Design
Strict separation of signing and verification responsibilities minimizes security risks and adapts to distributed microservice clusters:
- IdentityService (Exclusive Signer): Holds the sole RSA private key of the cluster, responsible for global AccessToken/RefreshToken signing and encryption — the only token issuer in the entire system.
- Downstream Microservices (Verifiers Only): Gateways, payment services, order services, and all business microservices load only the RSA public key. No signing permission, only token verification and identity parsing — achieving minimum privilege control.
2. K3s Secure Key Hosting & Container Mounting
Abandons insecure hardcoded and plaintext key configuration. Leverages native K3s security capabilities for encrypted storage and container injection.
2.1 K3s Secret Encrypted Injection
RSA public/private key pairs are externally generated and injected into K3s Secret resources. K3s automatically encrypts Secret data at rest, preventing plaintext leakage outside the cluster. Keys are only decryptable by authorized internal services.
2.2 PVC Persistent Volume Mounting
A dedicated K3s PVC persistent volume is mounted for IdentityService to permanently store encrypted key files. Keys survive Pod restarts, reconstruction, and version upgrades. Fine-grained file permission control ensures private key access is strictly limited to IdentityService, while downstream services only read public keys.
3. Full Key Lifecycle Automation
Custom key expiration policies automate key iteration, balancing security and operational stability:
- Independent expiration thresholds for signing and encryption keys to avoid long-term single-key leakage risks.
- Automatic generation of new key pairs when expiration thresholds are reached, synchronized to K3s Secret and PVC persistent directories.
- Newly started or rolling-updated containers automatically load the latest keys for zero-manual iteration.
4. Core Challenge: Smooth Compatibility for Key Rotation
The biggest production pain point of automatic key rotation is breaking compatibility during service rolling updates. During cluster iteration, old and new service versions coexist temporarily. Directly deleting old keys causes fatal failures: old services cannot verify new tokens, and new services cannot parse legacy tokens, leading to full-system authentication outages. This project solves this problem via customized source code optimization.
4.1 Core Source Code Capabilities
Full customized pipeline based on ConfigureOpenIddictOptions.cs, KeyRotationBackgroundService.cs, and KeyManagementController.cs:
- Multi-Key Persistent Loading: On startup, the service scans all keys in the PVC mount directory, loading the latest active key + all valid historical keys. Rotation only adds new keys without deleting old ones.
- Differentiated Sign/Verify Logic: Token signing uses only the latest private key for maximum security; token verification traverses all public keys (new and old) to ensure full compatibility.
- Zero-Downtime Hot Update: Background scheduled tasks trigger key rotation with atomic memory refresh. No service restart or Pod reconstruction required.
- Manual Emergency Backup: Exposed management interfaces for manual key rotation to handle emergencies and testing scenarios.
4.2 Cluster Smooth Transition Logic
- Rolling Update Compatibility: During batch Pod updates, old services retain historical keys to verify both legacy and new tokens; new services load full key sets for bidirectional compatibility.
- Gradual Cleanup Strategy: Old keys are never actively deleted. Invalid historical keys are cleaned manually only after all legacy tokens expire and old service versions are fully offline.
- Downstream Zero-Perception Adaptation: Gateways and business microservices require no code changes, automatically adapting to key iteration via native multi-public-key verification logic.
5. Production Deployment Specifications
- Do not manually delete historical keys in PVC to avoid legacy token verification failures.
- Execute key rotation during business low-traffic windows to avoid service jitter.
- Strictly isolate public/private key permissions; private keys are never distributed to downstream services.
- Coordinate with K3s rolling update strategies for stable cluster iteration.

---
🛠️ Technology Stack Selection
Adopts the latest .NET 10 ecosystem and C# 14 features to build a high-performance, standardized enterprise security infrastructure.
- .NET 10 Web API + C# 14: Primary constructors, field keywords, native async optimization, Native AOT compilation, and pipeline performance upgrades.
- OpenIddict 6.0+: Lightweight, highly customizable replacement for IdentityServer, fully controllable OAuth2/OIDC implementation.
- EF Core 10: Code-first development, compatible with SQL Server 2022 and mainstream relational databases.
- Redis + .NET10 HybridCache: Secondary caching architecture for permissions, tenants, and token blacklists, improving high-concurrency authentication throughput.
- MediatR: Implements CQRS pipeline, unifying interception, logging, transactions, and exception handling.

---
💻 Core API & Source Implementation Principles
The entire HttpApi layer contains only two controllers with zero business logic, zero SQL, zero direct cache operations — a pure enterprise-grade DDD + CQRS implementation template.
1. ConnectController (Standard OIDC Protocol Endpoints)
The system’s core security gateway, fully compliant with OIDC specifications for login, authentication, risk control, and single sign-out.
1.1 Token Exchange Endpoint /api/connect/token
- Request Constraint: Strict application/x-www-form-urlencoded form submission to prevent illegal JSON attacks.
- Workflow: Receive standard OpenIddict request → forward to ExchangeTokenCommand → return login success/failure/risk-forbidden results.
- Success Logic: Generate identity claims and trigger OpenIddict dual-token issuance.
- Failure Logic: Differentiate parameter errors, account freezing, and risk control interception.
- Design Highlight: Complete separation of protocol layer and business layer, supporting arbitrary authorization mode extensions.
1.2 UserInfo Trusted Endpoint /api/connect/userinfo
- Authentication Level: Requires valid AccessToken.
- Core Purpose: Secondary trusted verification for downstream gateways and microservices to prevent fake local tokens.
- Workflow: Parse user ID from token → execute GetUserInfoQuery → verify user status → return standardized identity claims.
- Response Fields: sub, username, user_type, merchant_id, store_id, status for unified gateway parsing.
- Risk Control: Automatically intercept frozen, deactivated, and blacklisted users.
1.3 Token Revoke Endpoint /api/connect/revoke
- Business Positioning: Emergency security fuse for payment risk control systems.
- Capability: Revoke both AccessToken and RefreshToken in real time.
- Mechanism: Leverage OpenIddict state machine to clear cache and database token records for second-level invalidation.
- Scenarios: Key leakage, device theft, merchant arrears, abnormal operator behavior.
1.4 Logout Endpoint /api/connect/logout
- Dual Logout Mechanism: Clear local Cookie session + destroy server-side SSO credentials.
- Effect: Completely eliminate login context residues to ensure secure sign-out.
2. ClientApplicationManagementController (Client Registration & Governance)
Manages dynamic access of SaaS applications and microservice clients to implement cluster access control.
2.1 Client Registration Endpoint /api/clientapplicationmanagement/register
- Input: AppName, ClientType, TenantId (tenant isolation).
- Workflow: DTO reception → encapsulate RegisterClientApplicationCommand → MediatR scheduling to generate client credentials.
- Security Mechanism: Client secrets are returned only once on first registration, with admin permission control for production environments.
- Scenarios: New microservice access, merchant custom application registration, third-party trusted system integration.
3. Unified Engineering Specifications
- Zero-Business Controllers: All logic sinks to Command/Query for independent unit testing.
- Protocol Standardization: Fully OIDC-compliant, compatible with all standard clients.
- Strong Security Isolation: Independent permissions for user login, service authorization, token revocation, and client registration.
- Financial-Grade Risk Control: Supports active token invalidation, account state interception, and tenant resource isolation.

---
🚀 Quick Start
1. Environment Dependencies
- .NET 10 SDK / Runtime
- SQL Server 2022
- Redis 6.0+
2. Deployment Steps
1. Clone the repository and configure database, Redis, and OpenIddict parameters.
2. Execute EF Core CodeFirst migration for automatic table creation.
3. Initialize super administrator, default roles, and basic permissions.
4. Start the service and test standard OIDC endpoints.

---
✨ Core Project Advantages
- Pure Standard Architecture: Strict DDD + CQRS + MediatR implementation without traditional layered architecture redundancy.
- Official Standard Protocol: Native OpenIddict OAuth2.0/OIDC, no wheel-reinventing, full ecosystem compatibility.
- Financial-Grade Security: Active token revocation, risk control interception, dual-token mechanism, multi-tenant isolation, one-time secret display.
- Cutting-Edge .NET10 Stack: C#14 syntax, HybridCache, Native AOT for maximum performance.
- Dual-Scenario Coverage: Integrated SaaS user authentication + microservice service authorization.
- High-Security Key Governance: K3s encrypted hosting + persistent PVC storage + full-lifecycle key rotation + smooth cross-version compatibility, solving industry cluster key iteration pain points.

---
📄 Open Source License
This project is open-sourced under the MIT License. Free for commercial and non-commercial use. Welcome to Star, Fork, and submit PRs for continuous optimization.
