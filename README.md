# Dokumentace pro Server 
Jan Šulák <br>
22.4.2024

---

### Obsah
* Úvod
* Nezbytná teorie
* Server
* Testování
* Popis implementace
* Bibliografie
  
### Úvod
V rámci mého programování v C# jsem používal .NET framework. Zejména třídy `TcpListener`, `TcpClient` a `UdpClient`. Tyto třídy jsou základem pro síťovou komunikaci v .NET aplikacích. Tyto třídy mi byly velkou oporou v porovnání s implementaci v C. Díky tomuto programu je možné provádět komunikaci mezi klienty a serverem pomocí obou protokolů TCP a UDP.

### Nezbytná teorie
#### TCP
Protokol TCP (Transmission Control Protocol) je základní komunikační protokol internetového protokolového souboru. Je to jeden z hlavních protokolů v sadě internetových protokolů a je zodpovědný za řízení a udržování spojení mezi hostitelskými počítači. TCP umožňuje dvěma hostům navázat spojení a vyměňovat si data. TCP zajišťuje spolehlivý, uspořádaný a bezchybný přenos dat mezi aplikacemi běžícími na hostitelských počítačích v síti.

#### UDP
Protokol UDP (User Datagram Protocol) je důležitou součástí sady internetových protokolů. Je známý pro svou jednoduchost a schopnost přenášet data bez nutnosti navázání spojení, což je hlavní rozdíl oproti protokolu TCP. UDP se často používá pro aplikace, které vyžadují rychlý přenos dat, jako jsou online hry nebo VoIP, ačkoliv to může znamenat, že některá data mohou být ztracena.

### Implementace
Program je rozdělen do několika tříd. Implementace UDP a TCP je na první pohled velmi podobná, ale zjistil jsem, že naprogramovat UDP je mnohem náročnější.
* TcpClient: Tato třída implementuje TCP server, který naslouchá příchozím spojením a zpracovává je.
* UdpClient: Zajišťuje funkčnost UDP serveru, který přijímá a odesílá UDP zprávy.
* Client: Třída reprezentující klienta, který může komunikovat se serverem pomocí TCP nebo UDP.
* Grammar: Obsahuje definice gramatiky pro komunikaci mezi klientem a serverem. To zahrnuje specifikaci formátu zpráv a jejich významu.
* TCPMessage, UDPMessage: Třídy pro reprezentaci zpráv, které jsou vyměňovány mezi klientem a serverem.
* Program: Obsahuje hlavní metodu programu, která inicializuje a spouští jak TCP, tak UDP servery, a zajišťuje jejich současnou funkcionalitu.

Klienti mohou posílat zprávy serveru, který je přijímá a reaguje na ně v souladu s definovanou gramatikou. Server je schopen správně zpracovávat příchozí spojení a zprávy pomocí obou protokolů současně. Třídy a jejich vztahy jsou popsány v následujícím diagramu.

### Testování
Při testování komunikace v síti jsem vytvořil vlastní server a klienta, kteří spolu komunikovali pomocí UDP i TCP protokolů. Tento způsob testování mi umožnil ověřit schopnost systému rychle a efektivně zpracovávat datové pakety.

### Bibliografie

UdpClient Class https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.udpclient?view=net-8.0<br>
TcpClient Class https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.tcpclient?view=net-8.0<br>
TcpListener Class https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.tcplistener?view=net-8.0<br>
UDP https://cs.wikipedia.org/wiki/User_Datagram_Protocol<br>
UDP https://www.sprava-site.eu/udp/<br>
TCP https://www.samuraj-cz.com/clanek/tcpip-model-encapsulace-paketu-vs-ramec/
