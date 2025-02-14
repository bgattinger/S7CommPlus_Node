class SessionManager {
    constructor(driver, retryInterval = 10000) {
        this.driver = driver;
        this.failedIPs = new Set();
        this.retryInterval = retryInterval;
        this.retryIntervalId = null;
    }

    async startSession(deviceIPs) {
        console.log("Starting session...");

        const results = await this.driver.Connect(deviceIPs.map(ip => ({
            ipAddress: ip,
            password: "default",  // Adjust based on your auth logic
            timeout: 1000
        })));

        results.forEach(result => {
            if (result.status === "Connection Failed") {
                console.log(`Failed to connect to ${result.IP}, will retry...`);
                this.failedIPs.add(result.IP);
            }
        });

        // Start background retries
        this.startBackgroundRetry();
    }

    async retryConnections() {
        if (this.failedIPs.size === 0) return;

        console.log("Retrying failed connections...");
        const ipsToRetry = Array.from(this.failedIPs);

        const results = await this.driver.Connect(ipsToRetry.map(ip => ({
            ipAddress: ip,
            password: "default",
            timeout: 1000
        })));

        results.forEach(result => {
            if (result.status === "Connection Successful") {
                console.log(`Successfully reconnected to ${result.IP}`);
                messageQueue.addMessage(`Device @ ${result.IP} is now connected.`);
                this.failedIPs.delete(result.IP);
            }
        });

        // If all failed connections are resolved, stop retrying
        if (this.failedIPs.size === 0) {
            clearInterval(this.retryIntervalId);
            this.retryIntervalId = null;
        }
    }

    startBackgroundRetry() {
        if (this.retryIntervalId !== null) return; // Prevent duplicate intervals
        this.retryIntervalId = setInterval(() => this.retryConnections(), this.retryInterval);
    }
}

const driver = new Driver();
const sessionManager = new SessionManager(driver);

async function main() {
    const userIPs = ["192.168.1.100", "192.168.1.101"]; // User provides these
    await sessionManager.startSession(userIPs);

    // Simulated main thread loop
    while (true) {
        if (messageQueue.hasMessages()) {
            console.log(messageQueue.getNextMessage());
        }

        // Simulate user interaction every few seconds
        await new Promise(resolve => setTimeout(resolve, 3000));
    }
}

main();