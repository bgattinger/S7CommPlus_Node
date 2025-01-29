
const edge = require('edge-js');
var Connect = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
    typeName: 'S7CommPlusDriverWrapper.DriverManager',
    methodName: 'Connect'
});
var GetDataBlockInfoList = edge.func({
    assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
    typeName: 'S7CommPlusDriverWrapper.DriverManager',
    methodName: 'GetDataBlockInfoList'
});

const express = require('express');
const bodyParser = require('body-parser');
const path = require('path');

const app = express();
const PORT = 3000;
var currentPlcIP;

// Middleware to parse form submission data from html page
app.use(bodyParser.urlencoded({ extended: true }));
app.use(bodyParser.json());


// Serve static files (HTML, CSS, JS) from the public directory
app.use(express.static(path.join(__dirname, 'public')));

// Route to serve HTML file
app.get('/', (req, res) => {
    res.sendFile(path.join(__dirname, 'public', 'index.html'));
});

// Handle form submission
app.post('/connect', (req, res) => {

    console.log(`Received IP Address: ${req.body.ipAddress}`);
  
    // initalize empty json response object
    let response = {};
    Connect({
        ipAddress: req.body.ipAddress,
        password: "", 
        timeout: 5000
    }, (error, connRes) => {
        console.log("Establishing connection to PLC");
        response.mssg1 = "Establishing connection to PLC @ IP: " + req.body.ipAddress;

        if (error) {
            console.log("Error connecting to PLC: ", error);
            response.mssg2 = "Error connecting to PLC @ IP: " + req.body.ipAddress;
            return res.json(response);
        }
        if (connRes != 0) {
            console.log("Unable to connect to PLC (connection code: ", connRes, ")");
            response.mssg2 = "Unable to connect to PLC @ IP: " + req.body.ipAddress + " (connection code: " + connRes + ")";
            return res.json(response);
        }
        
        console.log("Successful connection to PLC (connection code: ", connRes, ")");
        response.mssg2 = "Successful connection to PLC @ IP: "+ req.body.ipAddress + " (connection code: " +  connRes + ")";

        currentPlcIP = req.body.ipAddress;     //save IP address of currently connected PLC
        return res.json(response);
    });
    // experiment with this later
    //console.log("Am I running before Connect is finished? - No I don't appear to be because Connect lacks an await line");
});

app.post('/DatablockInfoList', (req,res) => {

    // initalize empty json response object
    let response = {};
    GetDataBlockInfoList(null, (error, dbInfoList) => {
        console.log("Retrieving Datablock Information List from PLC @ IP: " + currentPlcIP);
        response.mssg1 = "Retrieving Datablock Information List from PLC @ IP: " + currentPlcIP;

        if (error) {
            console.log("Error retrieiving DatablockInfo @ IP: " + currentPlcIP + " (error code: " + error+ ")");
            response.mssg2 = "Error retrieving Datablock Information List from PLC @ IP: " + currentPlcIP;
            return res.json(re);
        }

        console.log("Successfully retrieved Datablock Inforamation List from PLC @ IP: " + currentPlcIP);
        response.mssg2 = "Successfully retrieved Datablock Inforamation List from PLC @ IP: " + currentPlcIP;
        response.content = dbInfoList;
        
        //DEBUGGING
        //console.log(response); 

        return res.json(response);
    });
    
})

// Start server
app.listen(PORT, () => {
    console.log(`Server running at http://localhost:${PORT}`);
});