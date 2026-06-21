Zynq-7000 5-Port Ethernet DLPU & GUI Controller 
An FPGA-accelerated Data Link Processor Unit (DLPU) built on the AMD Xilinx Zynq-7000 SoC architecture, featuring a custom-designed Graphical User Interface (GUI) for real-time monitoring and network management. This repository contains the complete hardware-level Layer 2 packet processing pipeline implemented in the Programmable Logic (PL) and the software stacks interfacing with the control dashboard.
System OverviewThis project bridges high-performance hardware networking with a user-friendly management interface. The Zynq PL fabric handles line-rate packet routing and filtering across 5 physical Ethernet ports, while the ARM Processing System (PS) aggregates telemetry, processes configuration commands, and feeds data directly to the custom management GUI.
Key Capabilities
5x Gigabit Ethernet Switching: Low-latency, line-rate hardware packet forwarding and frame validation.
Hardware-Accelerated DLPU: Wire-speed Layer 2 engine for MAC address learning, VLAN parsing, and traffic filtering.
Deterministic Control Loop: AXI DMA interfaces connect the high-speed data plane directly to the software-driven telemetry layer.
Comprehensive Monitoring Dashboard: The custom GUI provides live visual insights into port status, packet counts, bandwidth utilization, and error diagnostics.
The Management GUI 
The custom-designed Graphical User Interface serves as the command center for the 5-port DLPU network switch. It communicates directly with the Zynq ARM processing cores to provide:
Live Port Matrix: Real-time visual status of link state (Up/Down), speed negotiation, and duplex mode for all 5 ports.
Traffic Analytics: Dynamic charts displaying ingress/egress throughput, bandwidth saturation, and packet size distribution.
Error Log & Diagnostics: Real-time capture of CRC errors, dropped packets, and collision metrics to monitor network health.
Static Config Matrix: Interactive interface to configure MAC filtering rules, toggle port power, and assign VLAN IDs on the fly.
