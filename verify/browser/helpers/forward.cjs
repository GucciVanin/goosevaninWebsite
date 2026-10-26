// Forwards localhost:443 inside the verification container to Caddy on the compose network, so the browsers use
// the real address https://localhost (the host name Caddy serves and the application's public address).
// Usage: node forward.cjs <caddy-ip>   Prints "ready" once it is listening.
const net = require('node:net');

const target = process.argv[2];
if (!target) {
  console.error('usage: node forward.cjs <caddy-ip>');
  process.exit(2);
}

let listening = 0;
for (const host of ['127.0.0.1', '::1']) {
  const server = net.createServer((client) => {
    const upstream = net.connect(443, target);
    client.pipe(upstream).pipe(client);
    const close = () => { client.destroy(); upstream.destroy(); };
    client.on('error', close);
    upstream.on('error', close);
  });
  server.on('error', (error) => console.error(`cannot listen on ${host}:443 (${error.code})`));
  server.listen(443, host, () => {
    listening += 1;
    if (listening === 1) console.log('ready');
  });
}
