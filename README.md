# PSValidator
This Application is a demo for PS Validator
It was build using Antigravity AI according to the requirements provided in the document
The document can be found in the following link: https://github.com/StarlineSoftware/PSValidator/blob/main/README.md

## How to run the application
1. Clone the repository
2. Open the folder in antigravity
3. App
        npm install
        npm run dev
        Open the browser and navigate to http://localhost:5166/swagger/index.html
4. Api
        dotnet restore
        dotnet build
        dotnet run
        Open the browser and navigate to http://localhost:5173

5. Deploy
        # Copy the example to create your actual config
        cp deploy.config.example deploy.config
        # Edit with your real values
        nano deploy.config
        # Run the deploy script
        ./deploy.sh
