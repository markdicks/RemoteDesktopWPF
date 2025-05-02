# RemoteDesktopWPF

## Overview
RemoteDesktopWPF is a secure, Windows Presentation Foundation (WPF)-based remote desktop application. This app allows users to remotely connect to and control another computer, viewing its screen and interacting with it as if they were physically present. The connection is secured with authentication and encrypted communications.

In addition to the remote access features, RemoteDesktopWPF now integrates with a custom-built .NET Web API for **user management**, enabling real-world-style **registration and login flows**.

## Features
- **Screen Sharing:** View and interact with the host computer's screen in real-time.
- **Secure Connection:** Encrypted communications using TLS/SSL to ensure all data transferred is secure.
- **User Authentication:** Users must register and log in before gaining access, using a secure API-based system.
- **Real-time Interaction:** Send keyboard and mouse inputs from the client to the host, allowing for seamless control.
- **API Integration:** All user management is handled via an external ASP.NET Core Web API built by the project author.

## Getting Started

### Prerequisites
- .NET Framework 4.8 or later
- Windows 10/11
- A running instance of the companion **TestAPI** (ASP.NET Core Web API)

### Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/markdicks/RemoteDesktopWPF.git
   ```
2. Open the solution in Visual Studio.
3. Build the solution to ensure all dependencies are properly set up.
4. Ensure the **TestAPI** project is also running (locally via Visual Studio or IIS Express).
5. Run the WPF application from Visual Studio or the built executable.

### API Setup (User Management)
This WPF app relies on a local API for authentication and user profile management. To enable this:

1. Clone and run the [Test_API project](https://github.com/markdicks/Test_API) (details available in its respective repository or solution).
2. Ensure it runs on a known HTTPS port (e.g., `https://localhost:7191`).
3. If running behind self-signed certificates, SSL validation is bypassed in development via `HttpClientHandler`.

## Usage

1. Start the **TestAPI**.
2. Launch the **WPF application**.
3. Use the **Register** screen to create a new user account.
4. Log in with your credentials via the **Login** screen.
5. Upon successful login, proceed to establish a remote connection to another host using IP address and password.
6. Control and interact with the host machine in real-time.

## Contributing

Contributions are what make the open-source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License
Distributed under the MIT License. See `LICENSE` for more information.

## Contact
Mark Dicks - [markdicks03@gmail.com](mailto:markdicks03@gmail.com)

## Acknowledgements
- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [Microsoft WPF Documentation](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
- [ASP.NET Core Web API Docs](https://learn.microsoft.com/en-us/aspnet/core/web-api/)

## Status
![Alt](https://repobeats.axiom.co/api/embed/3eed8aeb1c02d2f98be27e1c55be034cafdcda0a.svg "Repobeats analytics image")