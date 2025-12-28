// ---------------------------------------------------
//   ESP32 DS18B20 Temperature Logger with API
//   Endpoints:
//     /temp       -> live temperature
//     /log        -> full log download
//     /clearlog   -> erase log
//     /setsample?sec=60 -> set sample interval
// ---------------------------------------------------

#include <FS.h>
#include <LittleFS.h>
#include <ESP32WiFi.h>
#include <ESP32WebServer.h>
#include <WiFiManager.h>
#include <OneWire.h>
#include <DallasTemperature.h>
#include <time.h>
#include <ESP32mDNS.h>

// -------------------------
//   DS18B20 settings
// -------------------------
#define ONE_WIRE_BUS 2   // GPIO2 = D4 on Wemos D1 mini
OneWire oneWire(ONE_WIRE_BUS);
DallasTemperature sensors(&oneWire);

// -------------------------
//   Logging settings
// -------------------------
int SAMPLE_INTERVAL = 60;   // seconds (runtime modifiable)
unsigned long lastSample = 0;

// Web server
ESP32WebServer server(80);

// -------------------------
//   Append record to log
// -------------------------
void appendLog(float temp) {
    File f = LittleFS.open("/log.txt", "a");
    if (!f) return;

    time_t now = time(nullptr);
    f.printf("%lu;%.2f\n", now, temp);
    f.close();
}

// -------------------------
//   HTTP handlers
// -------------------------
void handleTemp() {
    sensors.requestTemperatures();
    float t = sensors.getTempCByIndex(0);
    server.send(200, "text/plain", String(t, 2));
}

void handleLog() {
    File f = LittleFS.open("/log.txt", "r");
    if (!f) {
        server.send(404, "text/plain", "No log found");
        return;
    }
    String data = f.readString();
    f.close();
    server.send(200, "text/plain", data);
}

void handleClearLog() {
    LittleFS.remove("/log.txt");
    server.send(200, "text/plain", "Log cleared");
}

// NEW: set sample interval via /setsample?sec=XXX
void handleSetSample() {
    if (!server.hasArg("sec")) {
        server.send(400, "text/plain", "Usage: /setsample?sec=<seconds>");
        return;
    }

    int s = server.arg("sec").toInt();
    if (s < 1 || s > 86400) {  // 1 sec to 24 hours
        server.send(400, "text/plain", "Range: 1–86400 seconds");
        return;
    }

    SAMPLE_INTERVAL = s;
    server.send(200, "text/plain", "Sample interval set to " + String(s) + " sec");
}


// -------------------------
//   Setup
// -------------------------
void setup() {
    Serial.begin(115200);
    delay(1000);

    // Start filesystem
    LittleFS.begin();

    // WiFi portal (SSID: ESP32-TempLogger)
    WiFiManager wm;
    wm.setConfigPortalTimeout(180);  // 3 minutes
    wm.autoConnect("ESP32-TempLogger");

    // Device will be accessible as esp32.local
    MDNS.begin("esp32");

    // NTP time sync
    configTime(0, 0, "pool.ntp.org", "time.nist.gov");

    // Start temperature sensor
    sensors.begin();

    // Register endpoints
    server.on("/temp", handleTemp);
    server.on("/log", handleLog);
    server.on("/clearlog", handleClearLog);
    server.on("/setsample", handleSetSample);
    server.begin();
    MDNS.addService("temperature", "tcp", 80);
    Serial.println("System ready.");
}

// -------------------------
//   Main loop
// -------------------------
void loop() {
    
    MDNS.update();
    server.handleClient();

    unsigned long now = millis();

    // Logging interval control
    if (now - lastSample >= SAMPLE_INTERVAL * 1000UL) {
        lastSample = now;

        sensors.requestTemperatures();
        float t = sensors.getTempCByIndex(0);

        Serial.printf("Temp: %.2f C  | Interval: %d sec\n", t, SAMPLE_INTERVAL);
        appendLog(t);
    }
}
