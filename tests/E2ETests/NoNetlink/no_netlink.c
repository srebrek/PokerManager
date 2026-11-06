// CI browser shim: fail NETLINK_ROUTE so Chromium ignores Docker veth churn (net::ERR_NETWORK_CHANGED).
#define _GNU_SOURCE
#include <dlfcn.h>
#include <errno.h>
#include <linux/netlink.h>
#include <sys/socket.h>

int socket(int domain, int type, int protocol)
{
    static int (*real_socket)(int, int, int);
    if (!real_socket)
    {
        real_socket = (int (*)(int, int, int))dlsym(RTLD_NEXT, "socket");
    }

    if (domain == AF_NETLINK && protocol == NETLINK_ROUTE)
    {
        errno = EAFNOSUPPORT;
        return -1;
    }

    return real_socket(domain, type, protocol);
}
