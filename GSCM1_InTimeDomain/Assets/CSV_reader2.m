clear all
% close all
pathname = "C:\Users\Administrator\Desktop\Aleksei\Parallel3DChaSi\GSCM1_InTimeDomain\Assets\H_freq";
colors = {'y','b','g','c','k'};
for k = 1:1
    fileAddress = pathname + k + ".csv";
    Hyy = csvread(fileAddress);
    
    Nfft = 1024;
    dt = 1/(Nfft*1000000);
    distance2 = 3e8*(0:dt:(Nfft-1)*dt);
    
    tt2 = 0.05:0.05:size(Hyy,1)*0.05;
    
    disp(size(Hyy,1));
    
    hyy=fliplr(ifft(Hyy,Nfft,2));%*sqrt(Nfft);
    PDPyy = zeros(size(Hyy,1),Nfft);
    
    for i = 1:size(Hyy,1)
        PDPyy(i,:) = abs(hyy(i,:)).^2;
        PDPyy(PDPyy<10^(-115/10))=0;
        PDPyy(:,750:end)=0;
        gyy(i,k)=sum(PDPyy(i,:));
    end
    
    figure
    h=pcolor(distance2(1:800), tt2(40:end), 10*log10(PDPyy(40:end,1:800)));
    set(h,'linestyle','none')
    caxis([-115 -70])
    title('PDP')
    xlabel('Delay distance (m)')
    ylabel('Time (s)')
    
    
    figure(4)
    hold on
    plot(7.58*tt2/max(tt2),10*log10(gyy(:,k)).',colors{k})
    grid on
end
gavg = sum(gyy,2)/5;
figure(4)
plot(7.58*tt2/max(tt2),10*log10(gavg).','r','linewidth',2)