class Interface {

    constructor() {
        this.mssgQueue = [];  // Queue to store messages
        this.isLogging = false;  // Flag to indicate whether we are currently logging
    }

    // Add a message to the queue
    addMessageToQueue(message) {
        if (this.isLogging) {
            // If we are already logging, just add the message to the queue
            this.mssgQueue.push(message);
        } else {
            // If we aren't logging, start logging immediately
            this.startLogging(message);
        }
    }

    // Process the queue and start logging messages
    async startLogging(message) {
        this.isLogging = true;  // Indicate we are logging
        try {
            await this.logMessage(message);  // Log the first message

            // After logging, check the queue for any other messages
            while (this.mssgQueue.length > 0) {
                const nextMessage = this.mssgQueue.shift();
                await this.logMessage(nextMessage);  // Log the next message
            }
        } catch (error) {
            console.error("Logging error:", error);
        } finally {
            // Reset the logging state when done
            this.isLogging = false;
        }
    }

    // Simulate logging a message (e.g., appending to a file)
    async logMessage(message) {
        return new Promise((resolve, reject) => {
            // Simulating logging by writing to a file (this could be a real file write operation)
            fs.appendFile('log.txt', message + '\n', (err) => {
                if (err) {
                    reject(err);
                } else {
                    resolve();
                }
            });
        });
    }
}
