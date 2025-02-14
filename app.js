const edge = require('edge-js');
const loki = require("lokijs");
const async = require('async');
const { interval, from, Subject } = require('rxjs');
const { map, mergeMap } = require('rxjs/operators');
const readlineSync = require('readline-sync');
const fs = require('fs');
const { error } = require('console');
const { Mutex } = require('async-mutex');
const ping = require('ping');


class Manager {

    static OUTLOG_FILENAME = "logging.txt";

    constructor(reconnectInterval = 10000, pingInterval = 60000) {
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

        // prepare logging output file
        await new Promise((resolve,reject) => {
            fs.writeFile(Manager.OUTLOG_FILENAME, '', (error) => {
                if (error) {
                    reject(error);
                }
                resolve();
            })
        });

        // get PLC Ips from user
        this.inputPlcIPs = this.ui.getUserInputPlcIPs()

        // attempt initial connections and track which succeeded and which failed
        const release = await this.mutex.acquire();                                     // DEV NOTE: Mutex probably not needed here but good to have jic and wont get in the way
        try {   
            let connResults = [];
            try {
                connResults = await this.driver.Connect(
                    [...this.inputPlcIPs].map(ip => ({
                        ipAddress: ip,
                        password: "",
                        timeout: 5000
                    }))
                );

            } catch (error) {
                throw new Error(error.message);
            }
            
            // track which initial connections were successful and which failed
            connResults.forEach(connRes => {

                //DEBUGGING
                //console.log("HERE1", connRes);

                if (connRes.status === S7CommPlusDriver.CONNSTAT_SUCCESS) {
                    this.connectedIPs.add(connRes.IP);
                } else {
                    this.disconnectedIPs.add(connRes.IP);
                }
            });
        } finally {
            release();
        }

        // start background reconnection operation (asynchronous)
        this.startReconnectDaemon();

        // start background ping operation (asynchronous)
        this.startPingDaemon();

        /*######################################### MAIN PROGRAM LOOP #########################################################*/
        while (true) {

            // deal with enqueued messages
            while (await this.ui.messageQueue.hasMessage()) {
                const message = JSON.stringify(await this.ui.messageQueue.dequeueMessage(), null, 2);

                await new Promise((resolve, reject) => {
                    fs.appendFile(
                        Manager.OUTLOG_FILENAME, 
                        message + '\n===\n\n', 
                        (error) => {
                            if (error) {
                                console.error("File write error:", error);
                                reject(error);
                            } else {
                                resolve();
                            }
                        }
                    );
                });
            }
            
            // Add a delay to prevent 100% CPU usage
            await new Promise(resolve => setTimeout(resolve, 100));
        }
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
            if (this.disconnectedIPs.size === 0) {
                return;
            }

            // queue up message that reconnection attempt is being made on failed PLC connections
            let disConnIPs_string = "";
            this.disconnectedIPs.forEach(ip => disConnIPs_string += (ip + "\n"));
            this.ui.messageQueue.enqueueMessage("Retrying Failed connections: \n" + disConnIPs_string);

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
                    this.ui.messageQueue.enqueueMessage(`PLC @ ${reConnResult.IP} is now connected`);
                } else {
                    this.ui.messageQueue.enqueueMessage(`PLC @ ${reConnResult.IP} failed to connect`);
                }
            });
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
            if (this.connectedIPs.size === 0) {
                return;
            }

            // queue up message that ping tests are being conducted on established PLC connections
            let connIPs_string = "";
            this.connectedIPs.forEach(ip => connIPs_string += (ip + "\n"));
            this.ui.messageQueue.enqueueMessage("Testing establised connections: \n" + connIPs_string);

            let pingResults;
            try {
                pingResults = await this.driver.Ping(
                    [...this.connectedIPs]
                );
            } catch (error) {
                throw new Error(error.message);
            }

            pingResults.forEach(pingResult => {
                if (pingResult.status === S7CommPlusDriver.CONN_ISDEAD) {
                    this.connectedIPs.delete(pingResult.IP);
                    this.driver.forgetConnection(pingResult.IP);
                    this.disconnectedIPs.add(pingResult.IP);
                    this.ui.messageQueue.enqueueMessage(`connection to PLC @ ${pingResult.IP} is down`);
                } else {
                    this.ui.messageQueue.enqueueMessage(`connection to PLC @ ${pingResult.IP} is up`);
                }
            });
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
                            error: new Error(`Unable to connect to PLC @ IP: ${connParam.ipAddress} (connection code: ${connRes})`)
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
                        SessID: sessID2
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

    static MessageQueue = class {
        constructor() {
            this.queue = [];
            this.mutex = new Mutex()
        }

        async enqueueMessage(mssg) {
            const release = await this.mutex.acquire();
            try {

                //DEBUGGING
                //console.log("HERE2", mssg);

                this.queue.push(mssg);
            } finally {
                release();
            }
            
        }

        async dequeueMessage() {
            const release = await this.mutex.acquire();
            try {

                const mssg = this.queue.shift();

                return mssg;
            } finally {
                release();
            }
        }

        async hasMessage() {
            const release = await this.mutex.acquire();
            try {

                //DEBUGGING
                //console.log("HERE3", this.queue.length > 0);

                return this.queue.length > 0;
            } finally {
                release();
            }
            
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
                    if (inputPlcIPs.length === 0) {
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
