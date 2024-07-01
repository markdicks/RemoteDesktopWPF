# RemoteDesktopWPF

## Overview
RemoteDesktopWPF is a secure, Windows Presentation Foundation (WPF)-based remote desktop application. This app allows users to remotely connect to and control another computer, viewing its screen and interacting with it as if they were physically present. The connection is secured with password protection, ensuring that only authorized users can access the remote system.

## Features
- **Screen Sharing:** View and interact with the host computer's screen in real-time.
- **Secure Connection:** Encrypted communications using TLS/SSL to ensure that all data transferred is secure.
- **Authentication:** Password-protected access to prevent unauthorized use.
- **Real-time Interaction:** Send keyboard and mouse inputs from the client to the host, allowing for seamless control.

## Getting Started
### Prerequisites
- .NET Framework 4.8 or later
- Windows 10/11

### Installation
1. Clone the repository:
```
git clone https://github.com/yourusername/RemoteDesktopWPF.git
```
2. Open the solution in Visual Studio.
3. Build the solution to ensure all dependencies are properly set up.
4. Run the application from Visual Studio or from the built executable.

## Usage
1. Start the server application on the host computer.
2. Open the client application on the remote computer.
3. Enter the host's IP address and the required password to establish a secure connection.
4. Once connected, you will see the host's desktop and can interact with it.

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
