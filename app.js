const edge = require('edge-js');
const loki = require("lokijs");
const async = require('async');
const { interval, from, Subject } = require('rxjs');
const { map, mergeMap } = require('rxjs/operators');
const readlineSync = require('readline-sync');
const fs = require('fs');
const { error, time } = require('console');
const { Mutex } = require('async-mutex');
const ping = require('ping');
const EventEmitter = require('events');


class Manager {
    constructor(reconnectInterval = 30000, pingInterval = 60000) {
        this.ui = new Interface();
        this.driver = new S7CommPlusDriver();

        this.inputPlcIPs = new Set();
        this.connectedIPs = new Set();
        this.disconnectedIPs = new Set();

        this.mutex = new Mutex();

        this.reconnectInterval = reconnectInterval;
        this.pingInterval = pingInterval;
        
        this.reconnectIntervalID = null;
        this.pingIntervalID = null;
    }










    async run() {

        // get PLC IPs from user input
        this.inputPlcIPs = this.ui.getUserInputPlcIPs()

        this.ui.messageQueue.enqueueMessage(
            `\n=== ${new Date()} ===\n` +
            "Initiating Connections to PLC(s) @ IP(s): \n" + 
            [...this.inputPlcIPs].map(ip => `\t${ip}`).join("\n")
        );

        // attempt initial connections and track which succeeded and which failed
        const initConnPromises = [...this.inputPlcIPs].map(async ip => {
            // Call single instance of Connect method for each single IP address asynchronously
            try {
                const initConnResults = await this.driver.Connect([{
                    ipAddress: ip,
                    password: "",
                    timeout: 5000
                }]);
                // Handle each single initial connection attempt as it resolves
                const initConnRes = initConnResults[0];

                if (initConnRes.status === S7CommPlusDriver.CONNSTAT_SUCCESS) {
                    this.connectedIPs.add(initConnRes.IP);
                    this.ui.messageQueue.enqueueMessage(
                        `\n=== ${new Date()} ===\n` +
                        "Successfully Connected to PLC @ IP: " + initConnRes.IP
                    );
                } else {
                    this.disconnectedIPs.add(initConnRes.IP);
                    this.ui.messageQueue.enqueueMessage(
                        `\n=== ${new Date()} ===\n` +
                        "Failed to Connect to PLC @ IP: " + initConnRes.IP
                    );
                }
            } catch (error) {
                this.disconnectedIPs.add(initConnRes.IP);
                this.ui.messageQueue.enqueueMessage(
                    `\n=== ${new Date()} ===\n` +
                    "Error on Connect to PLC @ IP: " + ip + "\n" +
                    "Error: " + error
                );
            }
        });
        // wait until all initial connection attempts resolve or reject
        await Promise.all(initConnPromises);

        // start background reconnection operation (asynchronous)
        this.startReconnectDaemon();

        // start background ping operation (asynchronous)
        this.startPingDaemon();

        /*######################################### MAIN PROGRAM LOOP #########################################################*/
        /*while (true) {
            // do other interface stuff here
            // Add a delay to prevent 100% CPU usage
            await new Promise(resolve => setTimeout(resolve, 100));
        }*/
        /*######################################### MAIN PROGRAM LOOP #########################################################*/

    }










    startReconnectDaemon() {
        // DEV NOTE: would like to implement exponential backoff for reconnection interval l8r (for funsies)

        // start background reconnect operation
        if (this.reconnectIntervalID !== null) {
            return;
        } else {
            this.reconnectIntervalID = setInterval(() => this.runReconnectDaemon(), this.reconnectInterval);
        }
    }
    async runReconnectDaemon() {
        const release = await this.mutex.acquire();
        try {
            if (this.disconnectedIPs.size === 0) { return; }

            // queue up message that reconnection attempt is being made on failed PLC connections
            this.ui.messageQueue.enqueueMessage(
                `\n=== ${new Date()} ===\n` +
                "Attempting to Reconnect to PLC(s) @ IP(s): \n" + 
                [...this.disconnectedIPs].map(ip => `\t${ip}`).join("\n")
            );


            // attempt to reconnect to disconnected PLC IPs
            const reConnPromises = [...this.disconnectedIPs].map(async (ip) => {
                // Call single instance of Connect method for each single IP address
                try {
                    const reConnResults = await this.driver.Connect([{
                        ipAddress: ip,
                        password: "",
                        timeout: 5000
                    }]);
                    // Handle each single reconnection attempt as it resolves
                    const reConnRes = reConnResults[0];

                    if (reConnRes.status === S7CommPlusDriver.CONNSTAT_SUCCESS) {
                        this.disconnectedIPs.delete(reConnRes.IP);
                        this.connectedIPs.add(reConnRes.IP);
                        this.ui.messageQueue.enqueueMessage(
                            `\n=== ${new Date()} ===\n` +
                            `Successfully Reconnected to PLC @ IP: ${reConnRes.IP}`
                        );
                    } else {
                        this.ui.messageQueue.enqueueMessage(
                            `\n=== ${new Date()} ===\n` +
                            `Failed to Reconnect to PLC @ IP: ${reConnRes.IP}`
                        );
                    }
                } catch (error) {
                    this.ui.messageQueue.enqueueMessage(
                        `\n=== ${new Date()} ===\n` +
                        "Error on Reconnect to PLC @ IP: " + ip + "\n" +
                        "Error: " + error
                    );
                }
            });
            // wait until all reconnection attempts resolve or reject
            await Promise.all(reConnPromises);
        } finally {
            release();
        }
    }







    startPingDaemon() {
        if (this.pingIntervalID !== null) {
            return;
        } else {
            this.pingIntervalID = setInterval(() => this.runPingDaemon(), this.pingInterval);
        }
    }
    async runPingDaemon() {
        const release = await this.mutex.acquire();
        try {
            if (this.connectedIPs.size === 0) { return; }

            // queue up message that ping tests are being conducted on established PLC connections
            this.ui.messageQueue.enqueueMessage(
                `\n=== ${new Date()} ===\n` +
                "Testing Connections to PLC(s) @ IP(s): \n" + 
                [...this.connectedIPs].map(ip => `\t${ip}`).join("\n")
            );


            // check connection to each PLC Ips
            const pingPromises = [...this.connectedIPs].map(async (ip) => {
                // Call single instance of Ping method for each single IP address
                try {
                    const pingResults = await this.driver.Ping(
                        [ip]
                    );
                    // Handle each single ping test as it resolves
                    const pingRes = pingResults[0];

                    if (pingRes.status === S7CommPlusDriver.CONN_ISALIVE) {
                        this.ui.messageQueue.enqueueMessage(
                            `\n=== ${new Date()} ===\n` +
                            `Retained Connection to PLC @ IP${pingRes.IP}`
                        );
                    } else {
                        this.connectedIPs.delete(pingRes.IP);
                        this.driver.forgetConnection(pingRes.IP);
                        this.disconnectedIPs.add(pingResult.IP);
                        this.ui.messageQueue.enqueueMessage(
                            `\n=== ${new Date()} ===\n` +
                            `Lost Connection to PLC @ IP ${pingRes.IP}`
                        );
                    }
                } catch (error) {
                    this.ui.messageQueue.enqueueMessage(
                        `\n=== ${new Date()} ===\n` +
                        "Error on Reconnect to PLC @ IP: " + ip + "\n" +
                        "Error: " + error
                    );
                }
            });
            // wait until all ping tests resolve or reject
            await Promise.all(pingPromises);
        } finally {
            release();
        }
    }
}















class S7CommPlusDriver {

    static CONNECT = edge.func({
        assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
        typeName: 'S7CommPlusDriverWrapper.DriverManager',
        methodName: 'Connect'
    });
    static CONNSTAT_SUCCESS = "Connection Successful";
    static CONNSTAT_FAIL = "Connection Failed";


    static DISCONNECT = edge.func({
        assemblyFile: '.\\S7CommPlusDllWrapper\\bin\\x64\\Debug\\S7CommPlusDllWrapper.dll', 
        typeName: 'S7CommPlusDriverWrapper.DriverManager',
        methodName: 'Disconnect'
    });
    static DISCONNSTAT_SUCCESS = "Disconnection Successful";
    static DISCONNSTAT_FAIL = "Disconnection Failed";

    static CONN_ISALIVE = true;
    static CONN_ISDEAD = false;

    constructor() {
        this.plcConns = new Map();
        this.mutex = new Mutex();
    }

    async Ping(ipArr) {
        const pingPromises = ipArr.map(ip => {
            return new Promise((resolve,reject) => {
                if (!this.plcConns.has(ip)) {
                    reject(new Error(`PLC connection @ IP ${ip} does not exist`));
                    return;
                } // else

                ping.sys.probe(ip, (isAlive) => {
                    if (isAlive) {
                        resolve({
                            status: S7CommPlusDriver.CONN_ISALIVE,
                            IP: ip
                        });
                    } else {
                        resolve({
                            status: S7CommPlusDriver.CONN_ISDEAD,
                            IP: ip
                        });
                    }
                });

                // Timeout rejection if no response withing 5 seconds
                setTimeout(
                    () => reject(new Error(`Ping request to PLC @ IP ${ip} timed out`)),
                    5000
                )
            });
        });
        return Promise.all(pingPromises);
    }

    async Connect(connParamArr) {
        const connPromises = connParamArr.map(connParam => {
            return new Promise((resolve,reject) => {

                // init input object
                let input = {
                    ipAddress: connParam.ipAddress,
                    password: connParam.password,
                    timeout: connParam.timeout
                };
                S7CommPlusDriver.CONNECT(input, async (error, output) => {
                    if (error) {
                        reject(error);
                        return;
                    } // else
                    // CONNECT executed succesfully

                    // parse output object
                    let connRes = output.Item1;
                    let sessID2 = output.Item2;

                    if (connRes !== 0) {
                        resolve({
                            status: S7CommPlusDriver.CONNSTAT_FAIL,
                            IP: connParam.ipAddress,
                            SessID: 0,
                            ConnCode: connRes
                        });
                        return;
                    } // else
                    
                    // track PLC connection
                    const release = await this.mutex.acquire();
                    try {
                        this.plcConns.set(connParam.ipAddress, sessID2);
                    } finally {
                        release(); // Ensure mutex is released
                    }

                    resolve({
                        status: S7CommPlusDriver.CONNSTAT_SUCCESS,
                        IP: connParam.ipAddress,
                        SessID: sessID2,
                        ConnCode: connRes
                    });
                });
            });
        });
        return Promise.all(connPromises);
    }

    async Disconnect(disConnIpArr) {
        const disConnPromises = disConnIpArr.map(ipAddress => {
            return new Promise((resolve,reject) => {
                if (!this.plcConns.has(ipAddress)) {
                    resolve({
                        status: "connection DNE",
                        IP: ipAddress,
                        SessID: 0,
                    });
                    return;
                } // else 

                // get corresponding sessionID for IP
                let sessID2 = this.plcConns.get(ipAddress);

                // init input object
                let input = {
                    sessionID2: sessID2
                }
                S7CommPlusDriver.DISCONNECT(input, async (error, output) => {
                    if (error) {
                        reject(error);
                        return;
                    } // else
                    // DISCONNECT executed successfully

                    // parse output object
                    let disConnRes = output;

                    if (disConnRes !== 1) {
                        reject(new Error(
                            "The application has attempted to terminate a PLC connection that the S7CommPlusDriverManger does not recognize"
                        ));
                        return;
                    } // else 

                    const release = await this.mutex.acquire();
                    try {
                        this.plcConns.delete(ipAddress);
                    } finally {
                        release(); // Ensure mutex is released
                    }

                    resolve({
                        status: "disconnected",
                        IP: ipAddress,
                        SessID: sessID2
                    });
                });
            });
        });
        return Promise.all(disConnPromises);
    }

    forgetConnection(ip) {
        this.plcConns.delete(ip);
    }
}















class Interface {

    static IP_VALID(ip) {
        const ipv4Regex = /^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$/;
        return ipv4Regex.test(ip);
    }

    static MessageQueue  = class {

        static OUTLOG_FILENAME = "MessageLog.txt";
        static HAVEMESSAGE_EVENT = 'haveMessage';

        constructor() {
            this.mssgQueue = [];
            this.isLogging = false;

            // prepare logging output file
            fs.writeFile(Interface.MessageQueue.OUTLOG_FILENAME, '', (error) => {
                if (error) {
                    reject(error);
                }
            });
        }

        enqueueMessage(mssg) {
            if (this.isLogging) {
                this.mssgQueue.push(mssg);
            } else {
                this.startLogging(mssg);
            }
        }

        async startLogging(mssg) {
            this.isLogging = true;
            try {
                // log first message
                await this.logMessage(mssg);
                
                // check if other messages have arrived in queue while logging and log them
                while (this.mssgQueue.length > 0) {
                    const nextMssg = this.mssgQueue.shift();
                    await this.logMessage(nextMssg);
                }
            } catch (error) {
                console.log("Logging Error: " + error);
            } finally {
                this.isLogging = false;
            }
        }

        async logMessage(mssg) {
            return new Promise((resolve,reject) => {
                fs.appendFile(Interface.MessageQueue.OUTLOG_FILENAME, 
                    `${mssg}\n`, 
                    (error) => {
                        if (error) {
                            reject(error);
                        } else {
                            resolve();
                        }
                    }
                );
            });
        }

    }

    constructor() {
        this.messageQueue = new Interface.MessageQueue();
    }

    getUserInputPlcIPs() {
        let inputPlcIPs = new Set();
        while (true) {
            console.log(
                "Please enter the IP addresses of the PLCs to connect to\n" +
                "\t(leave input field empty to continue)\n"
            );

            while (true) {
                let ip = readlineSync.question("IP > ");
                if (ip === '') {
                    if (inputPlcIPs.size === 0) {
                        console.log("No IP addresses have been entered.\nRestarting...");
                        return this.getUserInputPlcIPs(); // Restart if no IPs were entered
                    }
                    break;  // Stop asking when user inputs an empty string
                }
                if (Interface.IP_VALID(ip)) {
                    inputPlcIPs.add(ip);
                } else {
                    console.log("Invalid IP. Please enter a valid IP address.");
                }
            }

            console.log("connection attempt(s) will be made to PLCs at the following IP(s):")
            for (const ip of inputPlcIPs) {
                console.log(`\t${ip}`);
            }
            let resp = readlineSync.question("Begin Session? (y/n) >");
            if (resp === 'y') {
                console.log("Starting Session...")
                break;

            } else {    
                console.log("Aborting Session\nRestarting...");
            }
        }

        return inputPlcIPs;
    }
}


async function main() {
    try {
        const manager = new Manager();
        await manager.run();
    } catch (error) {
        console.log("\nAwww, Fiddlesticks! ya done goofed something:", "\n ---");
        console.log(error.stack,"\n ---\n");
    }
    
}
main();



/*
            let reConnResults;
            try {
                reConnResults = await this.driver.Connect(
                    [...this.disconnectedIPs].map(ip => ({
                        ipAddress: ip,
                        password: "",
                        timeout: 5000
                    }))
                ); 
            } catch (error) {
                throw new Error(error.message);
            }
            
           
            // queue up messages about results of reconnection attempt
            reConnResults.forEach(reConnResult => {
                if (reConnResult.status === S7CommPlusDriver.CONNSTAT_SUCCESS) {
                    this.disconnectedIPs.delete(reConnResult.IP);
                    this.connectedIPs.add(reConnResult.IP);
                    this.ui.messageQueue.enqueueMessage(
                        `\n=== ${new Date()} ===\n` +
                        `Successfully Reconnected to PLC @ IP: ${reConnResult.IP}`
                    );
                } else {
                    this.ui.messageQueue.enqueueMessage(
                        `\n=== ${new Date()} ===\n` +
                        `Failed to Reconnect to PLC @ IP: ${reConnResult.IP}`
                    );
                }
            });














            async runReconnectDaemon() {
        const release = await this.mutex.acquire();
        try {
            if (this.disconnectedIPs.size === 0) { return; }

            // queue up message that reconnection attempt is being made on failed PLC connections
            this.ui.messageQueue.enqueueMessage(
                `\n=== ${new Date()} ===\n` +
                "Attempting to Reconnect to PLC(s) @ IP(s): \n" + 
                [...this.disconnectedIPs].map(ip => `\t${ip}`).join("\n")
            );


            // attempt to reconnect to disconnected PLC IPs
            const reConnPromises = [...this.disconnectedIPs].map(ip => {
                // Call single instance of Connect method for each single IP address
                return this.driver.Connect([{
                    ipAddress: ip,
                    password: "",
                    timeout: 5000
                }])
                .then(reConnResults => {
                    // Handle each single reconnection attempt as it resolves
                    reConnRes = reConnResults[0];

                    if (reConnRes.status === S7CommPlusDriver.CONNSTAT_SUCCESS) {
                        this.disconnectedIPs.delete(reConnRes.IP);
                        this.connectedIPs.add(reConnRes.IP);
                        this.ui.messageQueue.enqueueMessage(
                            `\n=== ${new Date()} ===\n` +
                            `Successfully Reconnected to PLC @ IP: ${reConnRes.IP}`
                        );
                    } else {
                        this.ui.messageQueue.enqueueMessage(
                            `\n=== ${new Date()} ===\n` +
                            `Failed to Reconnect to PLC @ IP: ${reConnRes.IP}`
                        );
                    }
                })
                .catch(error => {
                    this.disconnectedIPs.add(initConnRes.IP);
                    this.ui.messageQueue.enqueueMessage(
                        `\n=== ${new Date()} ===\n`+ 
                        "Error on Reconnect to PLC @ IP: " + ip + "\n" +
                        "Error: " + error
                    );
                });
            });
            // wait until all reconnection attempts resolve or reject
            await Promise.all(reConnPromises);
        } finally {
            release();
        }
    }
            */