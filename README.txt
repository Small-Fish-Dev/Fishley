Hello ubre from the future or whoever is reading this, I'm sure you'll forget how to run this thing the moment you stop working on it for a week.

Everything through the terminal:

Make sure you're on the correct path Desktop/Fishley, so just "cd Desktop/Fishley"
To run the project "dotnet run fishley"
It uses the "fishley.service" to run automatically on startup, basically changing the directory to this project and running the above command.
You can stop the automatic process with "sudo systemctl stop fishley", make sure it's not running through this already before running with dotnet run or else you'll have two fishleys.
You can also restart it with "sudo systemctl start fishley", or disable, enable, etc... just change the keyword after systemctl
If you want to see the logs for any errors on your terminal from when fishley was running through the service, use "sudo journalctl -u fishley" and press on your END key to navigate to latest.
I use SQLite Entity Framework for the database, you can find the stuff in the SQLite folder, if you want to modify how Users or Fish are structured, or even add tables, or remove stuff etc... you always have to run the following afterwards to migrate the database:
"dotnet ef migrations add NameOfChange" and then "dotnet ef database update", both commands build the project so for it to work make sure there aren't any compile errors.
If "dotnet ef" doesn't work check your PATH and make sure that dotnet-ef is there, google it I already forgot how to do that.
Github stuff it's just "git status" to make sure you aren't missing any file or folder, "git add Folder/" or "git add file.cs" to start tracking the files, "git commit -a -m "My message"" to commit a change and "git push" to push, remember to put your password in.
If you want to connect through SSH to the raspberry do it through VSCode it's basically seamless, user is ubre@smallfish and the password is **** (You know it)