# IdentityService Distributed Permission Center · Core Technical Architecture Documentation

**IdentityService** is a lightweight, highly scalable, distributed unified authentication and authorization center built on \.NET 10\. Strictly following DDD \(Domain\-Driven Design\), CQRS \(Command Query Responsibility Segregation\) architecture and SOLID programming principles, it implements standard OAuth2\.0/OIDC protocols based on OpenIddict 6\.0\+\. It supports SaaS account password authentication, inter\-service authorization for microservices, and dual\-token mechanism, providing unified, secure and standardized identity authentication and permission management capabilities for distributed microservice architecture systems\.

✅ **Core Positioning**: Unified Identity Authentication, Distributed Permission Control, Microservice Security Gateway, SaaS Multi\-Tenant Identity Base

✅ **Applicable Scenarios**: Microservice clusters, SaaS multi\-tenant systems, front\-end and back\-end separated projects, distributed enterprise\-level systems

---

## 📋 Table of Contents

- Architecture Design \(DDD / CQRS / SOLID\)

- Core Authentication \& Authorization Mechanism

- Supported Authentication \& Authorization Models

- Token System \(JWT / AccessToken / RefreshToken\)

- Key Difficulties of RSA Key Containerized Management and Smooth Rotation \(Core Supplement\)

- Detailed Technology Selection

- Core Interface Source Code and Implementation Principles

- Quick Start

- Core Project Advantages

- Open Source License

---

# 🏗️ Architecture Design

Abandoning the bloated coupling problems of traditional layered architecture, this project adopts a three\-in\-one architecture system of **DDD Domain\-Driven Design \+ CQRS Read\-Write Separation \+ SOLID Design Principles** to achieve a highly cohesive, low\-coupling, scalable and maintainable enterprise\-level permission service architecture\.

## 1\. DDD Domain\-Driven Design

Taking "permissions, identities, tenants, roles, users" as the core domain models, it splits business domains with clear boundaries and completely solves the problems of mixed business and ambiguous responsibilities in traditional permission systems\.

- **Domain Layer**: Core business entities \(users, roles, permissions, tenants\), domain rules, and domain events\. It does not depend on any external framework and implements pure business logic to ensure business stability\.

- **Application Layer**: Coordinates domain logic and processes request orchestration without containing core business rules\. It relies on MediatR to achieve event decoupling and includes all **Command / Query** business use cases\.

- **Infrastructure Layer**: Responsible for database persistence, cache implementation, third\-party service docking, token generation and verification, and OpenIddict adaptation\.

- **Presentation / HttpApi Layer**: \.NET 10 Web API controllers, which only complete protocol reception, parameter forwarding and result response, with **zero business logic**\.

Meanwhile, based on the DDD domain event mechanism, asynchronous notification for scenarios such as user creation, role modification, permission update, and tenant configuration change is realized to support data synchronization in distributed systems\.

## 2\. CQRS Read\-Write Separation Architecture

It strictly distinguishes **read operations \(Query\)** and **write operations \(Command\)**, completely solving the problems of read\-write contention, performance bottlenecks and superimposed business complexity, which is fully aligned with the current source code structure\.

- **Command Write Operations**: Change operations such as token exchange, token revocation, client registration, and logout, ensuring data consistency and transaction integrity\.

- **Query Read Operations**: Query scenarios such as user identity information query and permission parsing, supporting cache acceleration\.

The CQRS scheduling is implemented through the MediatR mediator pattern\. Controllers do not directly call services or write business logic, and all processing is delegated to the Application layer, realizing **complete decoupling of requests and business**\.

## 3\. Implementation of SOLID Programming Principles

The project fully follows the five SOLID design principles and optimizes the code structure with C\# 14 new features to adapt to the current controller and business code structure:

- **Single Responsibility Principle**: ConnectController only processes OIDC protocol endpoints, and ClientApplicationController only processes client registration, with completely isolated responsibilities\.

- **Open/Closed Principle**: New login modes and authorization types can be extended by adding new Commands without modifying controller interfaces\.

- **Liskov Substitution Principle**: All infrastructure implementations \(database, cache, token service\) can be seamlessly replaced\.

- **Interface Segregation Principle**: Fine\-grained IRepository and business service interfaces with no redundant dependencies\.

- **Dependency Inversion Principle**: Controllers depend on MediatR abstractions rather than specific business implementations\.

---

# 🔐 Core Authentication \& Authorization Mechanism

Based on the standard **OAuth2\.0 / OIDC 1\.0** protocol and built on OpenIddict 6\.0\+, the system forms a standardized and customizable authentication and authorization system that balances universality and business customization capabilities, and adapts to SaaS tenant scenarios and microservice cluster scenarios\.

## 1\. Core Protocol Capabilities

- **OAuth2\.0**: A mainstream third\-party authorization and resource access protocol that solves resource authorization problems between microservices and between clients and servers\.

- **OIDC \(OpenID Connect\)**: An identity authentication protocol extended based on OAuth2\.0\. It adds standardized transmission of user identity information on the basis of authorization to realize unified identity login\.

As the underlying engine, OpenIddict fully takes over token issuance, verification, revocation and expiration state management\. The business layer only needs to handle **user legitimacy, tenant legitimacy and permission loading**\.

## 2\. Complete Authentication \& Authorization Link \(Source Code Corresponding Link\)

1. **Protocol Reception**: ConnectController\.Token interface receives standard OIDC form requests

2. **Business Verification**: ExchangeTokenCommand completes account, tenant, risk control and password verification

3. **Identity Loading**: Generates ClaimsPrincipal identity subject

4. **Protocol Issuance**: OpenIddict outputs standard AccessToken / RefreshToken

5. **Downstream Authentication**: Gateway/microservices verify tokens and call the UserInfo interface to obtain credible user information

6. **Security Destruction**: Supports active Revoke revocation and Logout cancellation

---

# ⚙️ Supported Authentication \& Authorization Models

For the two core scenarios of**SaaS end users** and **microservice backend clusters**, the project implements two differentiated authorization models, fully covering business access and service communication security\.

## 1\. SaaS Account Password Authentication Model \(Frontend Merchant/Operator\)

- **Entry**: /api/connect/token standard resource owner password credentials mode

- **Scenarios**: Merchant backend systems, cashiers, tenant operator login

- **Capabilities**: Multi\-tenant isolation, account risk control freezing, login verification, permission encapsulation

## 2\. Microservice Inter\-Client Authorization Model \(Service Communication\)

- **Entry**: ClientApplicationManagement client registration \+ service key authentication

- **Scenarios**: Microservice mutual calls, background tasks, internal system docking

- **Capabilities**: Dynamic client registration, service\-level isolation, prevention of illegal service access to the cluster

---

# 🎫 Token System: JWT \+ Dual\-Token Mechanism

The system adopts OpenIddict's native JWT generation mechanism and strictly implements the **AccessToken \+ RefreshToken dual\-token model**, meeting the high security requirements of financial and payment scenarios\.

## 1\. AccessToken \(Short\-Term Business Token\)

- Short validity period, used for authentication of all business interfaces

- Carries user, tenant, store, role and permission claims

- Extremely low leakage risk, suitable for front\-end storage

## 2\. RefreshToken \(Long\-Term Refresh Token\)

- Only used for token refresh, not participating in business authentication

- Persistent storage, supporting **active revocation**

- Enables users to renew tokens imperceptibly without repeated login

## 3\. Security Fuse Capability \(Unique Source Code Feature\)

Supports forced revocation of any token during operation, adapting to the "emergency red button" capability of the risk control system, solving the fatal flaw that traditional JWT cannot be invalidated actively\.

---

# 🛡️ Key Difficulties of RSA Key Containerized Management and Smooth Rotation \(Core Supplement\)

Based on the OpenIddict asymmetric encryption system, this project implements a production\-level microservice key security management scheme\. Relying on Tencent Cloud K3s clusters, it realizes encrypted key hosting, container persistent mounting, full lifecycle automatic rotation and cross\-service smooth compatibility, completely solving industry pain points such as plaintext key storage, one\-size\-fits\-all iterative updates, and cluster authentication exceptions in traditional solutions\.

## 1\. Core Design of Public\-Private Key Division of Labor

Adopting a standard asymmetric key separation mechanism, it strictly distinguishes signing and verification responsibilities to minimize the security risk of key leakage and adapt to distributed microservice cluster architecture:

- **Permission Center \(Exclusive Signing End\)**: Solely owns the RSA private key, fully responsible for the signature issuance and encryption of global AccessTokens and RefreshTokens, serving as the only token issuance source for the entire microservice cluster\.

- **All Downstream Microservices \(Unified Verification End\)**: Gateways, payment systems, order systems and all business microservices only load RSA public keys without token issuance permissions, which are only used for interface request token verification and user identity parsing, realizing minimum privilege control\.

## 2\. K3s Secure Key Hosting and Container Mounting Scheme

Abandoning the insecure mode of hard\-coded and plaintext stored keys in traditional configuration files, it realizes static key encryption, persistent retention and container imperceptible injection based on native Tencent Cloud K3s security capabilities\.

### 2\.1 K3s Secret Static Encrypted Key Injection

The RSA public and private key pairs required by the project are generated externally in compliance and then injected into K3s cluster Secret resources\. The K3s underlying layer automatically performs**static encrypted storage** on Secret data, making key plaintext unreadable outside the cluster\. All keys can only be decrypted and used by legitimate internal cluster services, eliminating key leakage and plaintext persistence risks from the infrastructure level\.

### 2\.2 PVC Container Persistent Mounting

An independent K3s PVC persistent storage volume is configured for the permission center to mount encrypted key files to fixed container directories\. It completely solves the key loss problem caused by temporary container storage, realizing permanent retention of key data after Pod restart, reconstruction and version update\. Meanwhile, file read and write permissions are finely controlled: only the permission center can read the private key, and downstream microservices can only read the public key, achieving hierarchical key isolation\.

## 3\. Full Lifecycle Key Management Mechanism

The system supports customizable key expiration policies, configures production\-adaptable key lifecycles, and realizes automatic iterative key updates while balancing security and operation and maintenance stability\.

- Configure reasonable expiration times for core signature keys and encryption keys to avoid leakage risks caused by long\-term use of a single key;

- When keys reach the expiration threshold, the system automatically generates new compliant key pairs and synchronously updates K3s Secrets and PVC persistent directories;

- Subsequently newly started and rolling\-updated containers automatically inject and load the latest keys to complete imperceptible key iteration without manual intervention\.

## 4\. Core Difficulty: Smooth Compatibility of New and Old Keys \(In\-Depth Source Code Analysis\)

The biggest production pain point of automatic key rotation is the **one\-size\-fits\-all incompatibility of new/old services and new/old keys**\. During the rolling update of microservice clusters, new and old service versions will inevitably coexist for a long time\. Directly deleting old keys and retaining only new keys will cause offline old services to fail to verify tokens signed by new keys and new services to fail to parse existing tokens signed by old keys, directly triggering global authentication collapse and large\-scale business exceptions\. This project completely solves this core pain point through customized source code modification\.

### 4\.1 Core Source Code Supported Capabilities

Relying on a complete set of core source codes including `ConfigureOpenIddictOptions.cs`, `KeyRotationBackgroundService.cs` and `KeyManagementController.cs`, it realizes core capabilities of multi\-key coexistence, differentiated effectiveness and zero\-downtime hot update iteration:

- **Full Key Loading Mechanism**: During service startup, the system automatically scans the PVC mounted key directory and loads the **latest active key \+ all unexpired historical keys**\. Key rotation only adds new keys without deleting or discarding old ones, retaining a complete set of keys in memory permanently\.

- **Differentiated Signing and Verification Logic**: Only the latest private key is used for token issuance to ensure the highest security and uniformity of new tokens; all new and old public keys in memory are traversed during token verification, and requests are allowed if any key verification passes, perfectly adapting to existing tokens and offline old services\.

- **Zero\-Downtime Hot Update Mechanism**: Background scheduled tasks automatically execute key rotation, and atomic memory refresh is realized through reloadable configurations without service restart or Pod reconstruction, enabling immediate new key take effect with zero business interruption\.

- **Manual Fallback Mechanism**: Provides interfaces to manually trigger key rotation, adapting to emergency key updates and test environment debugging scenarios, improving operation and maintenance fallback capabilities\.

### 4\.2 Microservice Cluster Smooth Transition Logic

- **Transition Compatibility During Rolling Update**: Pods are updated in batches in the cluster\. During the coexistence period of new and old services, old services retain historical key sets to normally verify new and old tokens; new services load full keys to achieve two\-way compatibility with all existing and new tokens\.

- **Progressive Cleaning Strategy**: Old keys are never actively deleted during key rotation\. Invalid historical keys are uniformly cleaned manually after all global existing tokens expire naturally and all old service versions are completely rolled offline, completely avoiding one\-size\-fits\-all risks\.

- **Downstream Service Imperceptible Adaptation**: All microservices such as gateways, order systems and payment systems do not require business code modification, and automatically adapt to key iteration relying on the native multi\-public\-key verification logic with zero modification and zero exceptions\.

## 5\. Production Implementation Specifications

- Manual deletion of historical keys in PVC is prohibited to avoid authentication failure of existing tokens and offline services;

- Key rotation is fixed to be executed during business low peak periods to avoid service jitter in high\-concurrency scenarios;

- Strict public\-private key permission isolation: private keys are exclusively held by the permission center and prohibited from being distributed to downstream microservices;

- Cooperate with K3s rolling update strategy to iterate services in batches to ensure stable cluster transition\.

---

# 🛠️ Detailed Technology Selection

The project adopts the latest \.NET 10 technology stack, makes full use of C\# 14 new features, and builds an enterprise\-level permission base with high\-performance and standardized open\-source frameworks\.

## 1\. Core Framework: \.NET 10 Web API \+ C\# 14

Fully leverages new version syntax to streamline project structure:

- **Primary Constructors**: Streamlines constructor injection for controllers, Commands and Queries

- **field Keyword**: Refined field permission control to improve domain model security

- Native asynchronous optimization, AOT compilation, and pipeline performance improvement

## 2\. OpenIddict 6\.0\+

Replaces the cumbersome IdentityServer with lightweight, customizable and protocol\-standard capabilities, fully controlling the entire OAuth2/OIDC process\.

## 3\. EF Core 10 \+ Database Adaptation

Developed with CodeFirst mode based on Entity Framework Core 10, supporting SQL Server 2022, and flexibly adaptable to mainstream relational databases to meet the deployment requirements of different environments\.

## 4\. Redis \+ \.NET10 HybridCache

Secondary cache architecture caches permissions, tenants and token blacklists, greatly improving authentication throughput and adapting to high\-concurrency distributed scenarios\.

## 5\. MediatR

Implements CQRS architecture, completely decoupling controllers and business\. All requests adopt the pipeline mode to realize unified interception, logging, transaction and exception handling\.

---

# 💻 Core Interface Source Code Implementation Principles \(Fully Aligned with Engineering Code\)

The entire HttpApi layer only has two controllers, with **zero business logic, zero SQL operations and zero cache operations**\. It strictly follows the CQRS \+ mediator architecture and serves as a standard enterprise\-level DDD implementation example\.

## 1\. ConnectController \(Four Standard OIDC Protocol Endpoints\)

As the core external security entrance of the system, it fully complies with OIDC protocol specifications, adapting to browser login, service authentication, risk control fusing and single sign\-on logout\.

### 1\.1 Token Exchange Interface /api/connect/token

- **Request Constraints**: Enforces `application/x-www-form-urlencoded` standard OIDC form submission to prevent illegal JSON attacks

- **Source Code Process**: Receive standard OpenIddict requests \- forward to **ExchangeTokenCommand** \- return login/prohibition/error results according to business conditions

- **Success Logic**: Return SignIn identity credentials, and OpenIddict automatically generates dual tokens

- **Failure Logic**: Distinguish Forbid login prohibition, parameter errors and account risk control freezing

- **Design Highlights**: Complete isolation of protocol layer and business layer, supporting arbitrary authorization mode expansion

### 1\.2 UserInfo Trusted User Information Interface /api/connect/userinfo

- **Authentication Level**: Valid OpenIddict AccessToken is mandatory

- **Core Function**: Secondary trusted verification for downstream microservice/payment gateways to prevent forged local tokens

- **Source Code Logic**: Parse subject \(user ID\) from token \- execute **GetUserInfoQuery** \- verify user activation status \- return standardized user claims

- **Return Structure**: sub, username, user\_type, merchant\_id, store\_id, status, adapted to unified gateway parsing

- **Risk Control Capability**: Automatically intercept frozen, logged\-out and blacklisted users under risk control

### 1\.3 Token Emergency Revocation Interface /api/connect/revoke

- **Business Positioning**: Emergency fusing entrance for payment risk control systems

- **Capabilities**: Supports revocation of AccessTokens and RefreshTokens

- **Principle**: Relying on the OpenIddict state machine, it automatically cleans up cached and database token records to achieve second\-level invalidation

- **Applicable Scenarios**: Key leakage, terminal theft, merchant arrears, abnormal operator behaviors

### 1\.4 Secure Logout Interface /api/connect/logout

- **Dual Cancellation Mechanism**: Clear local Cookie login status \+ destroy server\-side SSO credentials of OpenIddict

- **Effect**: Completely destroy the login context and eliminate residual single sign\-on status

## 2\. ClientApplicationManagementController \(Client Registration Management\)

Used for dynamic access of SaaS applications and microservice clients to realize cluster service access control\.

### 2\.1 Application Registration Interface /api/clientapplicationmanagement/register

- **Input Parameters**: AppName, ClientType, TenantId \(tenant isolation\)

- **Source Code Process**: DTO reception \- encapsulate **RegisterClientApplicationCommand** \- MediatR scheduling to generate client credentials

- **Security Mechanism**: Client keys are only returned for the first time, and platform administrator permission policies need to be enabled online

- **Scenarios**: New microservice access, merchant self\-developed application registration, third\-party trusted system access

## 3\. Unified Engineering Architecture Specifications \(Reflected in Source Code\)

- **Business\-Free Controllers**: All logic is sunk into Commands/Queries, supporting independent unit testing

- **Protocol Standardization**: Fully aligned with OIDC specifications, compatible with all standard clients

- **Strong Security Isolation**: Isolated permissions for user login, service authorization, token revocation and client registration

- **Financial\-Level Risk Control**: Supports active token invalidation, account status interception and tenant resource isolation

---

# 🚀 Quick Start

## 1\. Environment Dependencies

- \.NET 10 SDK / Runtime

- SQL Server 2022

- Redis 6\.0\+

## 2\. Deployment Steps

1. Clone the project and configure database, Redis and OpenIddict parameters

2. Execute EF Core CodeFirst migration to automatically create data tables

3. Initialize super administrator, default roles and basic permissions

4. Start the service and debug standard OIDC interfaces

---

# ✨ Core Project Advantages

- **Extremely Standard Architecture**: Pure DDD \+ CQRS \+ MediatR implementation without redundant traditional three\-layer architecture

- **Official Standard Protocol**: Native OpenIddict OAuth2\.0/OIDC, no repeated wheel creation, full ecological compatibility

- **Financial\-Level Security**: Supports active token revocation, risk control interception, dual\-token mechanism, tenant isolation and single key display

- **\.NET10 New Technology Stack**: C\#14 new syntax, HybridCache, AOT performance optimization

- **Dual\-Scenario Full Coverage**: Integrated SaaS user login and microservice service authorization

- **High\-Availability Key Security**: K3s encrypted hosting \+ PVC persistence \+ full\-lifecycle key rotation \+ smooth new/old key compatibility, completely solving cluster key iteration pain points

---

# 📄 Open Source License

This project is open sourced under the MIT license, free for commercial and non\-commercial use\. Welcome to Star, Fork and submit PRs for joint iterative optimization\.

---

# 6\. Full\-Lifecycle Architecture \& Key Rotation Execution Flowchart 

**Flowchart Description**: This end\-to\-end flowchart displays the complete technical link of the IdentityService distributed permission center, covering K3s key hosting, DDD/CQRS request processing, OIDC token issuance, multi\-key compatible verification, gateway traffic governance, microservice invocation, and zero\-downtime key smooth rotation\. It fully matches production cluster running logic and supports native GitHub Mermaid rendering\.

```mermaid

flowchart TB
    %% Define color styles
    classDef infra fill:#2c3e50,color:#fff
    classDef core fill:#3498db,color:#fff
    classDef security fill:#9b59b6,color:#fff
    classDef business fill:#27ae60,color:#fff
    classDef gateway fill:#f39c12,color:#fff
    classDef rotate fill:#e74c3c,color:#fff

    %% Infrastructure Key Layer
    A[K3s Cluster Infra]:::infra -- Secret Encrypted Injection + PVC Persistent Mount --> B[IdentityService Private Key HoldingOnly Signing Authority]:::security
    A --> C[Global Public Key SynchronizationDownstream Service Read-Only]:::security

    %% Key Rotation Background Mechanism
    B --> D[KeyRotationBackgroundServiceTimed Scan & Iteration]:::rotate
    D --> E{Key Expiration Threshold Reached?}
    E -- No --> F[Keep Active Key + Historical UnExpired Keys]
    E -- Yes --> G[Generate New RSA Key PairUpdate K3s Secret & PVC]:::rotate
    G --> H[Atomic Memory Hot RefreshZero Restart & Zero Downtime]:::rotate
    H --> F

    %% Client Request Entry
    I[Client / Microservice Client]:::business --> J[Gateway Traffic Entry]:::gateway
    J --> K[Gateway Multi-PublicKey VerificationNew/Old Key Compatibility Check]:::security

    %% Verification Branch
    K --> L{Token Verify Pass?}
    L -- No --> M[Return 401 Unauthorized / Forbid]
    L -- Yes --> N[Gateway Inject TraceId + Forward Request]:::gateway

    %% DDD + CQRS Processing Core
    N --> O[IdentityService HttpApi LayerZero Business Logic]:::core
    O --> P[MediatR CQRS Scheduling]:::core
    P --> Q{Write/Read Operation}
    Q -- Write(Command) --> R[ExchangeTokenCommand/RevokeCommandBusiness Verification & Transaction]:::core
    Q -- Read(Query) --> S[GetUserInfoQueryCache Accelerated Query]:::core

    %% OIDC Token Issuance & Management
    R --> T[OpenIddict 6.0 EngineDual-Token Generation]:::security
    T --> U[Issue AccessToken + RefreshTokenSigned by Latest Private Key]:::security
    S --> V[Return Standard User Identity Claims]

    %% Downstream Microservice Invocation
    U --> W[Business MicroservicesTrust Gateway Traffic, No Re-Verify]:::business
    V --> W
    W --> X[Execute Core Business Logic]:::business
    X --> Y[Return Result Full Link Back]

    %% Emergency Security Fuse
    Z[Risk Control System Trigger]:::security --> AA[Manual Token Revoke Interface]
    AA --> AB[OpenIddict State Machine CleanupSecond-Level Token Invalid]
    AB --> AC[Global Blacklist Cache Update]
    ```


