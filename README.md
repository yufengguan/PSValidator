# PSValidator
This application is a demo for PS Validator.
It was built using Antigravity AI according to Docs\PS_WebServiceValidator_Requirements_V02.md.

## How to Run the Application
1. Clone the repository
2. Open the folder in Antigravity
3. **App**
   ```bash
   npm install
   npm run dev
   ```
   Open the browser and navigate to http://localhost:5166/swagger/index.html
4. **API**
   ```bash
   dotnet restore
   dotnet build
   dotnet run
   ```
   Open the browser and navigate to http://localhost:5173

5. **Deploy**
   ```bash
   # Copy the example to create your actual config
   cp deploy.config.example deploy.config
   # Edit with your real values
   nano deploy.config
   # Run the deploy script
   ./deploy.sh
   ```
