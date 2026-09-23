# Deployment auf dem Raspberry Pi

## Bekannter Zielstand

Stand 23.09.2026: `pi@192.168.39.177`, Debian 12 (bookworm), ARM64 (`aarch64`,
64 Bit), angemeldete Desktop-Sitzung mit labwc und Xwayland (`DISPLAY=:0`).
Deploy- und Arbeitsverzeichnis: `/home/pi/PoolControl`.

Es wird ein Release-Paket für `linux-arm64` mit integrierter .NET-Laufzeit
verwendet. Das System benötigt weiterhin die nativen Grafikbibliotheken,
Schriften und Geräteberechtigungen. `pi` hat auf dem geprüften Gerät unter
anderem Zugriff über die Gruppen `gpio` und `i2c`. Ein Build für ARM32 wäre
für dieses Gerät falsch.

## SSH-Schlüsselzugang

Auf dem verwendeten Windows-Rechner liegt der Schlüssel unter
`$env:USERPROFILE\.ssh\id_ed25519_poolcontrol`; der öffentliche Teil endet auf
`.pub`. Der Schlüssel wurde bereits eingerichtet. Private Schlüssel gehören
nicht ins Repository. Die initiale Anmeldung zum Hinterlegen des öffentlichen
Schlüssels benötigt einen bereits funktionierenden Zugang zum Pi.

```powershell
# Nur bei erstmaliger Einrichtung, vorhandenen Schlüssel nicht überschreiben:
ssh-keygen -t ed25519 -f "$env:USERPROFILE\.ssh\id_ed25519_poolcontrol" -C poolcontrol-deploy
Get-Content "$env:USERPROFILE\.ssh\id_ed25519_poolcontrol.pub" | ssh pi@192.168.39.177 'umask 077; mkdir -p ~/.ssh; cat >> ~/.ssh/authorized_keys; chmod 700 ~/.ssh; chmod 600 ~/.ssh/authorized_keys'

# Passwortfreien Zugang und Zielarchitektur prüfen:
ssh -i "$env:USERPROFILE\.ssh\id_ed25519_poolcontrol" -o IdentitiesOnly=yes -o BatchMode=yes pi@192.168.39.177 'uname -m; getconf LONG_BIT; id'
```

Den Hostschlüssel beim ersten Verbinden anhand einer vertrauenswürdigen Quelle
prüfen; ein unerwartet geänderter Hostschlüssel muss geklärt werden.

## Konfigurationsdateien

- `appsettings.json`: MQTT, Logging und Persistenzpfad. Vor einem Update auf
  Übereinstimmung mit dem Ziel prüfen. Diese Datei kommt aus dem Buildpaket.
- `/home/pi/poolcontrolviewmodel.json`: ursprüngliche, vom Betreiber bereitgestellte
  Anlagenkonfiguration. Nur beim Erstdeployment als Quelle verwenden.
- `/home/pi/PoolControl/poolcontrolviewmodel.json`: aktuelle Anlagenkonfiguration;
  bei Updates beibehalten, da UI/MQTT und periodisches Speichern sie ändern.
- `winpoolcontrolviewmodel.json`: lokale Windows-Konfiguration; kein Ersatz für
  die Linux-Anlagendatei.
- UI-Präferenzen: `Environment.SpecialFolder.LocalApplicationData/PoolControl/ui.json`,
  auf diesem Linux-Typ üblicherweise `/home/pi/.local/share/PoolControl/ui.json`.

Beim Erstdeployment wurde die Originaldatei zusätzlich als
`/home/pi/poolcontrolviewmodel.json.predeploy-20260923` gesichert, unverändert in
das Deploy-Verzeichnis kopiert und vor dem Start mit `cmp` verglichen. Erst
danach wurde das für Messmodelle ungültige `Distance.IntervalInSec = 0` auf
ausdrücklichen Betreiberwunsch in der Deploy-Datei auf `60` gesetzt. Die Datei
unter `/home/pi` wurde dabei nicht angepasst.

## Bauen und Übertragen

Im Repository auf Windows mit installiertem .NET-10-SDK:

```powershell
dotnet publish PoolControl.csproj -c Release -r linux-arm64 --self-contained true -p:PublishTrimmed=false -p:UsedAvaloniaProducts= -o artifacts/deploy/linux-arm64
if ($LASTEXITCODE -ne 0) { throw 'Publish fehlgeschlagen' }
tar -czf artifacts/deploy/poolcontrol-linux-arm64.tar.gz -C artifacts/deploy/linux-arm64 .
if ($LASTEXITCODE -ne 0) { throw 'Archivierung fehlgeschlagen' }
scp -i "$env:USERPROFILE\.ssh\id_ed25519_poolcontrol" -o IdentitiesOnly=yes artifacts/deploy/poolcontrol-linux-arm64.tar.gz pi@192.168.39.177:/home/pi/
if ($LASTEXITCODE -ne 0) { throw 'Übertragung fehlgeschlagen' }
```

`UsedAvaloniaProducts=` unterdrückt die Buildstatistik für eingeschränkte
Buildumgebungen. Trimming ist für dieses Deployment explizit ausgeschaltet.
Die Linux-Anlagenkonfiguration wird vom Projekt nicht ins Publish-Verzeichnis kopiert.

## Aktualisieren

Zuerst die laufende Anwendung über die Oberfläche regulär beenden. Ein offenes
klassisches Konfigurationsfenster muss zuvor geschlossen werden. Den Abschluss
abwarten (`Application shutdown completed` im Log). So werden aktuelle Werte
gespeichert und Relais/GPIO geordnet geschlossen. Nicht über laufende Dateien
extrahieren. `systemctl stop`/SIGTERM ist kein zugesicherter Ersatz für den
Fenster-Schließpfad dieser Version.

Danach auf dem Pi, als Benutzer `pi`:

```sh
set -eu
if pgrep -x PoolControl >/dev/null; then
    echo 'PoolControl zuerst regulär beenden' >&2
    exit 1
fi
backup="/home/pi/PoolControl.before-$(date +%Y%m%d-%H%M%S)"
test ! -e "$backup"
cp -a /home/pi/PoolControl "$backup"
tar -xzf /home/pi/poolcontrol-linux-arm64.tar.gz -C /home/pi/PoolControl
chmod +x /home/pi/PoolControl/PoolControl
cmp "$backup/poolcontrolviewmodel.json" /home/pi/PoolControl/poolcontrolviewmodel.json
python3 -m json.tool /home/pi/PoolControl/poolcontrolviewmodel.json >/dev/null
echo "Backup: $backup"
```

Beim Erstdeployment stattdessen ein neues Verzeichnis anlegen, das Paket
extrahieren und **vor dem ersten Start** die Betreiberdatei kopieren:

```sh
cp -p /home/pi/poolcontrolviewmodel.json /home/pi/PoolControl/poolcontrolviewmodel.json
```

Nicht bei späteren Updates erneut aus der Originaldatei kopieren: Das würde
zwischenzeitlich gespeicherte Einstellungen verlieren.

## Start und Prüfung

Der bisher verwendete Start ist eine transiente systemd-Benutzereinheit. Er
nutzt die Umgebung der angemeldeten Desktop-Sitzung und das richtige
Arbeitsverzeichnis:

```sh
systemctl --user show-environment
systemd-run --user --unit=poolcontrol --property=WorkingDirectory=/home/pi/PoolControl /home/pi/PoolControl/PoolControl
systemctl --user show poolcontrol -p ActiveState -p SubState -p MainPID -p ExecMainStatus
DISPLAY=:0 xwininfo -root -tree | grep PoolControl
sudo journalctl _SYSTEMD_USER_UNIT=poolcontrol.service --since '2 minutes ago' --no-pager
```

Falls die Einheit von einem Fehlstart noch existiert, zuerst Ursache und
Protokoll prüfen; anschließend `systemctl --user reset-failed poolcontrol` vor
dem erneuten Start. Keine zweite Steuerungsinstanz parallel starten.

Mindestens den Startabschluss und einen vollständigen Messzyklus abwarten.
Prüfen: JSON geladen, Fenster vorhanden, Prozess weiter aktiv, MQTT verbunden,
keine Startvalidierungsfehler, erwartete Sensorantworten. Messfehler dürfen
nicht allein wegen eines aktiven Prozesses als behoben gelten.

Bekannte Beobachtungen und Grenzen stehen im
[Checkpoint](Checkpoint-Modern-UI.md). Die User-Journalabfrage lieferte auf dem
Gerät ohne `sudo` keine Einträge; das Anwendungslog im Arbeitsverzeichnis ist
eine zusätzliche Quelle.

**Kein Autostart nach Neustart eingerichtet.** `systemd-run` erzeugt keine
dauerhaft installierte oder aktivierte Service-Datei. Ein solcher Autostart
ist ein separater nächster Schritt und braucht die passende Desktop-Umgebung.

## Wiederherstellung

Anwendung regulär beenden und Stillstand prüfen. Den gewünschten Backup-Pfad
aus dem vorherigen Deployment ausdrücklich auswählen. Das aktuelle
Deploy-Verzeichnis zunächst unter einem neuen Namen aufbewahren, dann das
Backup nach `/home/pi/PoolControl` zurückkopieren. Nicht blind über eine
laufende Installation schreiben. Entscheiden, ob die aktuelle Konfiguration
beibehalten oder ebenfalls auf den Backup-Stand zurückgesetzt werden soll;
beide Versionen vorher sichern. Danach denselben Start- und Prüfablauf verwenden.

Im Laufe dieser Sitzung wurden Backups unter
`/home/pi/PoolControl.backup-*-20260923` erstellt. Das zuletzt vor diesem
Checkpoint installierte Paket enthält die erweiterten Sensor-Textfelder;
das Backup unmittelbar davor heißt `PoolControl.backup-sensor-settings-20260923`.
